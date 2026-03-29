<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
import ToolIcon from '@/components/icons/ToolIcon.vue';
import { api } from '@/services/api';
import { taskHub } from '@/services/taskHub';
import type { TaskStartedEvent, TaskProgressEvent, TaskCompletedEvent, TaskCancelledEvent, TaskFailedEvent } from '@/types';

interface DriveInfo {
  name: string;
  volumeLabel: string;
  driveType: string;
  totalSize: number;
  availableFreeSpace: number;
  driveFormat: string;
}

interface FoundFile {
  name: string;
  path: string;
  directory: string;
  sizeBytes: number;
  lastModified: string;
}

interface FolderGroup {
  directory: string;
  files: FoundFile[];
  expanded: boolean;
}

const drives = ref<DriveInfo[]>([]);
const currentPath = ref<string>('C:\\');
const customPathInput = ref<string>('');
const filterInput = ref<string>('*.*');

const isScanning = ref(false);
const isDeleting = ref(false);
const activeTaskId = ref<string | null>(null);
const currentStatus = ref<string>('');

const items = ref<FoundFile[]>([]);
const selectedPaths = ref<Set<string>>(new Set());

onMounted(async () => {
    // Load drives
    try {
        const result = await api.executeOperation('get-drives');
        if (result.success && result.data && result.data.drives) {
            drives.value = result.data.drives as DriveInfo[];
            if (drives.value.length > 0) {
                currentPath.value = drives.value[0].name;
            }
        }
    } catch (e) {
        console.error('Failed to get drives', e);
    }

    // Connect hub and register event listeners
    await taskHub.connect();
    taskHub.on('TaskStarted', onTaskStarted);
    taskHub.on('TaskProgress', onTaskProgress);
    taskHub.on('TaskCompleted', onTaskCompleted);
    taskHub.on('TaskCancelled', onTaskCancelled);
    taskHub.on('TaskFailed', onTaskFailed);
});

onUnmounted(() => {
    taskHub.off('TaskStarted', onTaskStarted);
    taskHub.off('TaskProgress', onTaskProgress);
    taskHub.off('TaskCompleted', onTaskCompleted);
    taskHub.off('TaskCancelled', onTaskCancelled);
    taskHub.off('TaskFailed', onTaskFailed);
});

// Format bytes
function formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

const groupedFiles = computed(() => {
    const groups = new Map<string, FolderGroup>();
    for (const f of items.value) {
        if (!groups.has(f.directory)) {
            groups.set(f.directory, { directory: f.directory, files: [], expanded: true });
        }
        groups.get(f.directory)!.files.push(f);
    }
    return Array.from(groups.values()).sort((a, b) => a.directory.localeCompare(b.directory));
});

function toggleGroup(group: FolderGroup) {
    group.expanded = !group.expanded;
}

function toggleSelection(path: string) {
    const newKeys = new Set(selectedPaths.value);
    if (newKeys.has(path)) {
        newKeys.delete(path);
    } else {
        newKeys.add(path);
    }
    selectedPaths.value = newKeys;
}

function toggleGroupSelection(group: FolderGroup) {
    const allSelected = group.files.every(f => selectedPaths.value.has(f.path));
    const newKeys = new Set(selectedPaths.value);
    if (allSelected) {
        group.files.forEach(f => newKeys.delete(f.path));
    } else {
        group.files.forEach(f => newKeys.add(f.path));
    }
    selectedPaths.value = newKeys;
}

async function deleteSelected() {
    const paths = Array.from(selectedPaths.value);
    if (!paths.length) return;
    
    if (!confirm(`Are you sure you want to delete ${paths.length} files?`)) return;
    
    isDeleting.value = true;
    try {
        const res = await api.executeOperation('delete-files', { paths });
        if (res.data && res.data.deleted) {
            const deleted = new Set(res.data.deleted as string[]);
            items.value = items.value.filter(f => !deleted.has(f.path));
            
            const newKeys = new Set(selectedPaths.value);
            for (const p of deleted) {
                newKeys.delete(p);
            }
            selectedPaths.value = newKeys;
        }
        currentStatus.value = res.message || 'Deletion complete';
    } catch (e) {
        console.error(e);
        currentStatus.value = 'Failed to delete files';
    } finally {
        isDeleting.value = false;
    }
}

async function deleteFile(path: string) {
    if (!confirm(`Are you sure you want to delete this file?\n${path}`)) return;
    
    isDeleting.value = true;
    try {
        const res = await api.executeOperation('delete-files', { paths: [path] });
        if (res.data && res.data.deleted && (res.data.deleted as string[]).includes(path)) {
            items.value = items.value.filter(f => f.path !== path);
            const newKeys = new Set(selectedPaths.value);
            newKeys.delete(path);
            selectedPaths.value = newKeys;
        }
        currentStatus.value = res.message || 'Deletion complete';
    } catch (e) {
        console.error(e);
        currentStatus.value = 'Failed to delete file';
    } finally {
        isDeleting.value = false;
    }
}

async function startScan(targetPath: string) {
    if (isScanning.value && activeTaskId.value) {
        await api.cancelTask(activeTaskId.value);
    }

    items.value = [];
    selectedPaths.value = new Set();
    currentPath.value = targetPath;
    const currentFilter = filterInput.value || '*.*';
    currentStatus.value = `Searching ${currentFilter} in ${targetPath}...`;
    isScanning.value = true;

    try {
        const res = await api.startTask('find-files', { targetPath, filter: currentFilter });
        activeTaskId.value = res.taskId;
        await taskHub.subscribeToTask(res.taskId);
    } catch (e) {
        console.error(e);
        isScanning.value = false;
        currentStatus.value = 'Failed to start scan';
    }
}

async function stopScan() {
    if (isScanning.value && activeTaskId.value) {
        currentStatus.value = 'Stopping...';
        await api.cancelTask(activeTaskId.value);
    }
}

function scanDrive(drive: DriveInfo) {
    customPathInput.value = '';
    startScan(drive.name);
}

function scanCustom() {
    const pathToScan = customPathInput.value || currentPath.value;
    if (!pathToScan) return;
    startScan(pathToScan);
}

// Hub event handlers
function onTaskStarted(data: TaskStartedEvent) {
    if (data.taskId !== activeTaskId.value) return;
}

function onTaskProgress(data: TaskProgressEvent) {
    if (data.taskId !== activeTaskId.value) return;
    
    currentStatus.value = data.progress.statusMessage || '';
    
    if (data.progress.customData && data.progress.customData.items) {
        const newItems = data.progress.customData.items as any[];
        items.value = newItems.map(i => ({
            name: i.name || i.Name,
            path: i.path || i.Path,
            directory: i.directory || i.Directory,
            sizeBytes: i.sizeBytes !== undefined ? i.sizeBytes : i.SizeBytes,
            lastModified: i.lastModified || i.LastModified
        }));
    }
}

function onTaskCompleted(data: TaskCompletedEvent) {
    if (data.taskId !== activeTaskId.value) return;
    isScanning.value = false;
    currentStatus.value = data.result.message || 'Scan completed.';
    if (data.result.data && data.result.data.items) {
        const newItems = data.result.data.items as any[];
        items.value = newItems.map(i => ({
            name: i.name || i.Name,
            path: i.path || i.Path,
            directory: i.directory || i.Directory,
            sizeBytes: i.sizeBytes !== undefined ? i.sizeBytes : i.SizeBytes,
            lastModified: i.lastModified || i.LastModified
        }));
    }
}

function onTaskCancelled(data: TaskCancelledEvent) {
    if (data.taskId !== activeTaskId.value) return;
    isScanning.value = false;
    currentStatus.value = 'Scan cancelled.';
}

function onTaskFailed(data: TaskFailedEvent) {
    if (data.taskId !== activeTaskId.value) return;
    isScanning.value = false;
    currentStatus.value = `Scan failed: ${data.error}`;
}

</script>

<template>
  <div class="tool-view">
    <header class="tool-header">
      <div class="tool-title-row">
        <ToolIcon name="find-files" :size="28" class="tool-header-icon" />
        <div>
          <h1>Find Files</h1>
          <p class="tool-subtitle">
            Search for files by filter and optionally delete them.
          </p>
        </div>
      </div>
      
      <div class="tool-controls">
        <div class="drives-list">
          <button 
            v-for="drive in drives" :key="drive.name"
            class="action-btn"
            :class="{ active: currentPath === drive.name }"
            @click="scanDrive(drive)"
            :disabled="isScanning"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="4" width="20" height="16" rx="2" ry="2"></rect><line x1="6" y1="12" x2="6.01" y2="12"></line><line x1="10" y1="12" x2="10.01" y2="12"></line></svg>
            {{ drive.name }}
          </button>
        </div>
        
        <div class="input-row">
          <input 
            v-model="customPathInput" 
            type="text" 
            placeholder="Or enter custom path... e.g. C:\Users"
            class="text-input"
            @keyup.enter="scanCustom"
            :disabled="isScanning"
            style="flex: 2"
          />
          <input 
            v-model="filterInput" 
            type="text" 
            placeholder="Filter e.g. *.tmp"
            class="text-input"
            @keyup.enter="scanCustom"
            :disabled="isScanning"
            style="flex: 1"
          />
          <button class="action-btn primary" @click="scanCustom" :disabled="isScanning || (!customPathInput && !currentPath)">
            Search
          </button>
        </div>
      </div>
    </header>

    <div class="tool-body">
      <div class="scan-area" v-if="items.length > 0 || isScanning">
        <div class="scan-toolbar">
            <div class="scan-status">
                <span v-if="isScanning || isDeleting" class="spinner"></span>
                <span class="status-text">{{ currentStatus }}</span>
            </div>
            
            <div class="scan-actions">
                <span v-if="selectedPaths.size > 0" class="selection-count">{{ selectedPaths.size }} selected</span>
                <button v-if="selectedPaths.size > 0" class="action-btn danger" @click="deleteSelected" :disabled="isDeleting">
                    Delete Selected
                </button>
                <button v-if="isScanning" class="action-btn danger outline" @click="stopScan">
                    Stop
                </button>
            </div>
        </div>

        <div class="results-list">
            <div v-for="group in groupedFiles" :key="group.directory" class="folder-group">
                <div class="folder-header" @click="toggleGroup(group)">
                    <div class="folder-title">
                        <svg :class="{'expanded': group.expanded}" class="chevron" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"></polyline></svg>
                        <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path></svg>
                        <span class="folder-name">{{ group.directory }}</span>
                        <span class="folder-count">({{ group.files.length }})</span>
                    </div>
                    <div class="folder-actions" @click.stop>
                        <input type="checkbox" :checked="group.files.every(f => selectedPaths.has(f.path))" @change="toggleGroupSelection(group)" class="checkbox" />
                    </div>
                </div>
                
                <div v-if="group.expanded" class="folder-files">
                    <div v-for="file in group.files" :key="file.path" class="file-item">
                        <input type="checkbox" :checked="selectedPaths.has(file.path)" @change="toggleSelection(file.path)" class="checkbox" />
                        
                        <div class="file-info">
                            <span class="file-name">{{ file.name }}</span>
                            <span class="file-meta">{{ formatBytes(file.sizeBytes) }} • {{ new Date(file.lastModified).toLocaleString() }}</span>
                        </div>
                        
                        <button class="icon-btn action-delete" @click="deleteFile(file.path)" title="Delete" :disabled="isDeleting">
                            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><line x1="10" y1="11" x2="10" y2="17"></line><line x1="14" y1="11" x2="14" y2="17"></line></svg>
                        </button>
                    </div>
                </div>
            </div>
        </div>
      </div>

      <div class="placeholder-card" v-else>
        <div class="placeholder-icon-wrapper">
          <ToolIcon name="find-files" :size="48" />
        </div>
        <h3>Search for Files</h3>
        <p>
          Select a drive or enter a custom path, specify a filter, and hit search to find files.
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tool-view {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.tool-header {
  padding: var(--space-6) var(--space-8) var(--space-4);
  background: var(--surface-bg);
  border-bottom: 1px solid var(--surface-border);
  box-shadow: var(--surface-shadow);
  z-index: 10;
}

.tool-title-row {
  display: flex;
  align-items: flex-start;
  gap: var(--space-4);
  margin-bottom: var(--space-5);
}

.tool-header-icon {
  color: var(--accent);
  margin-top: 2px;
  flex-shrink: 0;
}

.tool-header h1 {
  font-size: var(--text-2xl);
  font-weight: 700;
  margin-bottom: var(--space-1);
}

.tool-subtitle {
  color: var(--text-secondary);
  font-size: var(--text-sm);
}

.tool-controls {
    display: flex;
    flex-direction: column;
    gap: var(--space-4);
}

.drives-list {
    display: flex;
    gap: var(--space-3);
    flex-wrap: wrap;
}

.input-row {
    display: flex;
    gap: var(--space-3);
    max-width: 800px;
}

.text-input {
    padding: var(--space-2) var(--space-3);
    border: 1px solid var(--surface-border);
    border-radius: var(--radius-md);
    background: var(--content-bg);
    color: var(--text-primary);
    font-size: var(--text-sm);
    outline: none;
    transition: border-color var(--duration-fast);
}

.text-input:focus {
    border-color: var(--accent);
    background: var(--surface-bg);
}

.text-input:disabled {
    opacity: 0.6;
}

.tool-body {
  flex: 1;
  padding: var(--space-6) var(--space-8);
  overflow-y: auto;
}

/* Common Buttons */
.action-btn {
    display: inline-flex;
    align-items: center;
    gap: var(--space-2);
    padding: var(--space-2) var(--space-4);
    background: var(--surface-bg);
    border: 1px solid var(--surface-border);
    border-radius: var(--radius-md);
    color: var(--text-primary);
    font-weight: 500;
    font-size: var(--text-sm);
    transition: all var(--duration-fast);
}

.action-btn:hover:not(:disabled) {
    background: var(--content-bg);
    border-color: var(--text-muted);
}

.action-btn.active {
    background: var(--accent-subtle);
    border-color: var(--accent);
    color: var(--accent-text);
}

.action-btn.primary {
    background: var(--accent);
    border-color: var(--accent);
    color: white;
}

.action-btn.primary:hover:not(:disabled) {
    background: var(--accent-hover);
}

.action-btn.danger {
    background: var(--error);
    border-color: var(--error);
    color: white;
}

.action-btn.danger:hover:not(:disabled) {
    background: #dc2626; /* darker error */
}

.action-btn.danger.outline {
    background: transparent;
    border-color: var(--error);
    color: var(--error);
}

.action-btn.danger.outline:hover:not(:disabled) {
    background: rgba(239, 68, 68, 0.1);
}

.action-btn:disabled {
    opacity: 0.5;
    cursor: default;
}

.icon-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 32px;
    height: 32px;
    background: transparent;
    color: var(--text-secondary);
    border-radius: var(--radius-md);
    transition: all var(--duration-fast);
}

.icon-btn:hover:not(:disabled) {
    background: var(--surface-border);
    color: var(--text-primary);
}

.icon-btn.action-delete:hover:not(:disabled) {
    background: rgba(239, 68, 68, 0.1);
    color: var(--error);
}


/* Spinner */
.spinner {
    display: inline-block;
    width: 16px;
    height: 16px;
    border: 2px solid var(--surface-border);
    border-top: 2px solid var(--accent);
    border-radius: 50%;
    animation: spin 1s linear infinite;
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

/* Scanning Area */
.scan-area {
    display: flex;
    flex-direction: column;
    gap: var(--space-6);
}

.scan-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    background: var(--surface-bg);
    padding: var(--space-3) var(--space-4);
    border-radius: var(--radius-lg);
    box-shadow: var(--surface-shadow);
}

.scan-actions {
    display: flex;
    align-items: center;
    gap: var(--space-4);
}

.selection-count {
    font-size: var(--text-sm);
    color: var(--text-secondary);
    font-weight: 500;
}

.scan-status {
    display: flex;
    align-items: center;
    gap: var(--space-2);
}

.status-text {
    font-size: var(--text-sm);
    color: var(--text-muted);
    font-family: var(--font-mono);
}

/* Results */
.results-list {
    display: flex;
    flex-direction: column;
    gap: var(--space-4);
}

.folder-group {
    background: var(--surface-bg);
    border-radius: var(--radius-md);
    border: 1px solid var(--surface-border);
    overflow: hidden;
    box-shadow: var(--surface-shadow);
}

.folder-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: var(--space-3) var(--space-4);
    background: var(--content-bg);
    cursor: pointer;
    user-select: none;
}

.folder-header:hover {
    background: var(--surface-border);
}

.folder-title {
    display: flex;
    align-items: center;
    gap: var(--space-2);
    color: var(--text-primary);
    font-weight: 600;
}

.chevron {
    transition: transform var(--duration-fast);
    color: var(--text-muted);
}

.chevron.expanded {
    transform: rotate(90deg);
}

.folder-name {
    font-family: var(--font-mono);
    font-size: var(--text-sm);
}

.folder-count {
    color: var(--text-muted);
    font-weight: normal;
    font-size: var(--text-sm);
}

.folder-files {
    display: flex;
    flex-direction: column;
    border-top: 1px solid var(--surface-border);
}

.file-item {
    display: flex;
    align-items: center;
    gap: var(--space-4);
    padding: var(--space-2) var(--space-4);
    border-bottom: 1px solid var(--surface-border);
}

.file-item:last-child {
    border-bottom: none;
}

.file-item:hover {
    background: var(--content-bg);
}

.file-info {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-width: 0;
}

.file-name {
    font-size: var(--text-sm);
    color: var(--text-primary);
    font-weight: 500;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.file-meta {
    font-size: var(--text-xs);
    color: var(--text-muted);
}

.checkbox {
    width: 16px;
    height: 16px;
    cursor: pointer;
    accent-color: var(--accent);
}

/* Empty State */
.placeholder-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: var(--space-12) var(--space-8);
  background: var(--surface-bg);
  border: 1px dashed var(--surface-border);
  border-radius: var(--radius-lg);
  min-height: 300px;
}

.placeholder-icon-wrapper {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 80px;
  height: 80px;
  border-radius: var(--radius-xl);
  background: var(--accent-subtle);
  color: var(--accent);
  margin-bottom: var(--space-6);
}

.placeholder-card h3 {
  font-size: var(--text-lg);
  color: var(--text-primary);
  margin-bottom: var(--space-2);
}

.placeholder-card p {
  color: var(--text-muted);
  font-size: var(--text-sm);
  max-width: 400px;
  line-height: var(--leading-relaxed);
}
</style>
