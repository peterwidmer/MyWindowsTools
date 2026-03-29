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

interface DirStats {
  name: string;
  path: string;
  sizeBytes: number;
  fileCount: number;
  folderCount: number;
  isDirectory: boolean;
}

interface ScanTotals {
  sizeBytes: number;
  files: number;
  folders: number;
}

const drives = ref<DriveInfo[]>([]);
const pathHistory = ref<string[]>([]);
const currentPath = ref<string>('C:\\');
const customPathInput = ref<string>('');

const isScanning = ref(false);
const activeTaskId = ref<string | null>(null);
const currentStatus = ref<string>('');

const totals = ref<ScanTotals>({ sizeBytes: 0, files: 0, folders: 0 });
const items = ref<DirStats[]>([]);

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

    // Add global event listeners for navigation
    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('mouseup', handleMouseUp);
});

onUnmounted(() => {
    taskHub.off('TaskStarted', onTaskStarted);
    taskHub.off('TaskProgress', onTaskProgress);
    taskHub.off('TaskCompleted', onTaskCompleted);
    taskHub.off('TaskCancelled', onTaskCancelled);
    taskHub.off('TaskFailed', onTaskFailed);

    // Remove global event listeners
    window.removeEventListener('keydown', handleKeyDown);
    window.removeEventListener('mouseup', handleMouseUp);
});

function handleKeyDown(e: KeyboardEvent) {
    // Check if backspace was pressed and we're not in an input field
    const target = e.target as HTMLElement;
    const isInput = target && (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA');

    if (e.key === 'Backspace' && !isInput) {
        e.preventDefault();
        goUp();
    }
}

function handleMouseUp(e: MouseEvent) {
    // Button 3 is the standard "Back" button on mice
    if (e.button === 3) {
        e.preventDefault();
        goBack(); // Or goUp(), depending on if you want it to go back in history or to the parent folder. Given "like Windows Explorer", goBack is usually right for the back button, and parent directory is up. We'll use goBack() for history.
    }
}

// Format bytes
function formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function getPercent(size: number): number {
    if (!totals.value.sizeBytes) return 0;
    return (size / totals.value.sizeBytes) * 100;
}

async function startScan(targetPath: string) {
    if (isScanning.value && activeTaskId.value) {
        await api.cancelTask(activeTaskId.value);
    }

    items.value = [];
    totals.value = { sizeBytes: 0, files: 0, folders: 0 };
    currentPath.value = targetPath;
    currentStatus.value = `Starting scan for ${targetPath}...`;
    isScanning.value = true;

    try {
        const res = await api.startTask('directory-size', { targetPath });
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
    pathHistory.value = [];
    customPathInput.value = '';
    startScan(drive.name);
}

function scanCustom() {
    if (!customPathInput.value) return;
    pathHistory.value = [];
    startScan(customPathInput.value);
}

function drillDown(item: DirStats) {
    if (!item.isDirectory || item.name === '<Files in root>') return;
    pathHistory.value.push(currentPath.value);
    startScan(item.path);
}

function goBack() {
    if (pathHistory.value.length === 0) return;
    const prev = pathHistory.value.pop();
    if (prev) {
        startScan(prev);
    }
}

function goUp() {
    // Quick parse to go up one directory level
    let p = currentPath.value;
    if (p.endsWith('\\') || p.endsWith('/')) {
        p = p.substring(0, p.length - 1);
    }
    const lastSlash = Math.max(p.lastIndexOf('\\'), p.lastIndexOf('/'));
    if (lastSlash > 0) { // Not just C:
        const parent = p.substring(0, lastSlash + 1);
        pathHistory.value.push(currentPath.value);
        startScan(parent);
    }
}

// Hub event handlers
function onTaskStarted(data: TaskStartedEvent) {
    if (data.taskId !== activeTaskId.value) return;
}

function onTaskProgress(data: TaskProgressEvent) {
    if (data.taskId !== activeTaskId.value) return;
    
    currentStatus.value = data.progress.statusMessage || '';
    
    if (data.progress.customData) {
        const cd = data.progress.customData as any;
        if (cd.totals) {
            totals.value.sizeBytes = cd.totals.sizeBytes || cd.totals.SizeBytes || 0;
            totals.value.files = cd.totals.files || cd.totals.Files || 0;
            totals.value.folders = cd.totals.folders || cd.totals.Folders || 0;
        }
        if (cd.items && Array.isArray(cd.items)) {
            items.value = cd.items.map((i: any) => ({
                name: i.name || i.Name || i.name, // .NET might uppercase depending on casing config
                path: i.path || i.Path,
                sizeBytes: i.sizeBytes !== undefined ? i.sizeBytes : i.SizeBytes,
                fileCount: i.fileCount !== undefined ? i.fileCount : i.FileCount,
                folderCount: i.folderCount !== undefined ? i.folderCount : i.FolderCount,
                isDirectory: i.isDirectory !== undefined ? i.isDirectory : i.IsDirectory,
            }));
        }
    }
}

function onTaskCompleted(data: TaskCompletedEvent) {
    if (data.taskId !== activeTaskId.value) return;
    isScanning.value = false;
    currentStatus.value = data.result.message || 'Scan completed.';
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

const canGoUp = computed(() => {
    let p = currentPath.value;
    if (p.endsWith('\\') || p.endsWith('/')) {
        p = p.substring(0, p.length - 1);
    }
    return Math.max(p.lastIndexOf('\\'), p.lastIndexOf('/')) > 0;
});
</script>

<template>
  <div class="tool-view">
    <header class="tool-header">
      <div class="tool-title-row">
        <ToolIcon name="directory-size" :size="28" class="tool-header-icon" />
        <div>
          <h1>Directory Size</h1>
          <p class="tool-subtitle">
            Analyze folder sizes and find the largest directories on your system.
          </p>
        </div>
      </div>
      
      <div class="tool-controls">
        <div class="drives-list">
          <button 
            v-for="drive in drives" :key="drive.name"
            class="action-btn"
            :class="{ active: currentPath === drive.name && !pathHistory.length }"
            @click="scanDrive(drive)"
            :disabled="isScanning"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="4" width="20" height="16" rx="2" ry="2"></rect><line x1="6" y1="12" x2="6.01" y2="12"></line><line x1="10" y1="12" x2="10.01" y2="12"></line></svg>
            {{ drive.name }}
          </button>
        </div>
        
        <div class="custom-path-box">
          <input 
            v-model="customPathInput" 
            type="text" 
            placeholder="Or enter custom path... e.g. C:\Users"
            class="path-input"
            @keyup.enter="scanCustom"
            :disabled="isScanning"
          />
          <button class="action-btn primary" @click="scanCustom" :disabled="isScanning || !customPathInput">
            Scan
          </button>
        </div>
      </div>
    </header>

    <div class="tool-body">
      <div class="scan-area" v-if="items.length > 0 || isScanning">
        
        <div class="scan-toolbar">
            <div class="path-navigation">
                <button class="icon-btn" @click="goBack" :disabled="pathHistory.length === 0 || isScanning" title="Go Back">
                    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="15 18 9 12 15 6"></polyline></svg>
                </button>
                <button class="icon-btn" @click="goUp" :disabled="!canGoUp || isScanning" title="Go Up One Level">
                    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="18 15 12 9 6 15"></polyline></svg>
                </button>
                <h3 class="current-path">{{ currentPath }}</h3>
            </div>
            
            <div class="scan-actions">
                <div class="scan-status">
                    <span v-if="isScanning" class="spinner"></span>
                    <span class="status-text">{{ currentStatus }}</span>
                </div>
                <button v-if="isScanning" class="action-btn danger outline" @click="stopScan">
                    Stop Scan
                </button>
            </div>
        </div>
        
        <div class="scan-summary">
            <div class="stat-box">
                <span class="stat-label">Total Size</span>
                <span class="stat-value">{{ formatBytes(totals.sizeBytes) }}</span>
            </div>
            <div class="stat-box">
                <span class="stat-label">Files</span>
                <span class="stat-value">{{ totals.files.toLocaleString() }}</span>
            </div>
            <div class="stat-box">
                <span class="stat-label">Folders</span>
                <span class="stat-value">{{ totals.folders.toLocaleString() }}</span>
            </div>
        </div>

        <div class="items-list">
            <div 
                v-for="item in items" 
                :key="item.path" 
                class="list-item"
                :class="{ 'is-clickable': item.isDirectory && item.name !== '<Files in root>' && !isScanning }"
                @click="item.isDirectory && item.name !== '<Files in root>' && !isScanning ? drillDown(item) : null"
            >
                <div class="item-icon">
                    <svg v-if="item.isDirectory" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path></svg>
                    <svg v-else xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z"></path><polyline points="13 2 13 9 20 9"></polyline></svg>
                </div>
                
                <div class="item-content">
                    <div class="item-header">
                        <span class="item-name">{{ item.name }}</span>
                        <div class="item-stats">
                            <span class="item-size">{{ formatBytes(item.sizeBytes) }}</span>
                            <span class="item-percent">{{ getPercent(item.sizeBytes).toFixed(1) }}%</span>
                        </div>
                    </div>
                    <div class="item-progress-track">
                        <div class="item-progress-fill" :style="{ width: getPercent(item.sizeBytes) + '%' }"></div>
                    </div>
                </div>
            </div>
        </div>
      </div>

      <div class="placeholder-card" v-else>
        <div class="placeholder-icon-wrapper">
          <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none"
            stroke="currentColor" stroke-width="1.25" stroke-linecap="round" stroke-linejoin="round">
            <path d="M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z" />
            <circle cx="12" cy="13" r="2" />
          </svg>
        </div>
        <h3>Select a Drive or Path</h3>
        <p>
          Pick a drive above or enter a custom path to start analyzing directory sizes.
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

.custom-path-box {
    display: flex;
    gap: var(--space-3);
    max-width: 500px;
}

.path-input {
    flex: 1;
    padding: var(--space-2) var(--space-3);
    border: 1px solid var(--surface-border);
    border-radius: var(--radius-md);
    background: var(--content-bg);
    color: var(--text-primary);
    font-size: var(--text-sm);
    outline: none;
    transition: border-color var(--duration-fast);
}

.path-input:focus {
    border-color: var(--accent);
    background: var(--surface-bg);
}

.path-input:disabled {
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

.icon-btn:disabled {
    opacity: 0.3;
    cursor: default;
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

.path-navigation {
    display: flex;
    align-items: center;
    gap: var(--space-2);
}

.current-path {
    margin-left: var(--space-2);
    font-size: var(--text-lg);
    color: var(--text-primary);
    font-family: var(--font-mono);
}

.scan-actions {
    display: flex;
    align-items: center;
    gap: var(--space-4);
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
    max-width: 300px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.scan-summary {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: var(--space-4);
}

.stat-box {
    background: var(--surface-bg);
    padding: var(--space-4);
    border-radius: var(--radius-lg);
    box-shadow: var(--surface-shadow);
    display: flex;
    flex-direction: column;
    gap: var(--space-1);
}

.stat-label {
    font-size: var(--text-xs);
    text-transform: uppercase;
    color: var(--text-muted);
    font-weight: 600;
    letter-spacing: 0.05em;
}

.stat-value {
    font-size: var(--text-2xl);
    font-weight: 700;
    color: var(--accent-text);
}

.items-list {
    display: flex;
    flex-direction: column;
    gap: var(--space-2);
}

.list-item {
    display: flex;
    align-items: center;
    gap: var(--space-4);
    background: var(--surface-bg);
    padding: var(--space-3) var(--space-4);
    border-radius: var(--radius-md);
    box-shadow: var(--surface-shadow);
    transition: transform var(--duration-fast), box-shadow var(--duration-fast);
}

.list-item.is-clickable {
    cursor: pointer;
}

.list-item.is-clickable:hover {
    transform: translateY(-1px);
    box-shadow: var(--surface-shadow-lg);
}

.item-icon {
    width: 40px;
    height: 40px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--accent-subtle);
    color: var(--accent);
    border-radius: var(--radius-md);
    flex-shrink: 0;
}

.item-content {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: var(--space-2);
}

.item-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-end;
}

.item-name {
    font-weight: 600;
    color: var(--text-primary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.item-stats {
    display: flex;
    align-items: baseline;
    gap: var(--space-3);
    flex-shrink: 0;
}

.item-size {
    font-size: var(--text-sm);
    font-weight: 600;
    color: var(--text-secondary);
}

.item-percent {
    font-size: var(--text-xs);
    font-weight: 600;
    color: var(--text-muted);
    width: 44px;
    text-align: right;
}

.item-progress-track {
    height: 6px;
    background: var(--content-bg);
    border-radius: var(--radius-full);
    overflow: hidden;
}

.item-progress-fill {
    height: 100%;
    background: linear-gradient(90deg, var(--accent), #8b5cf6);
    border-radius: var(--radius-full);
    transition: width var(--duration-base);
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
  grid-column: 1 / -1;
  margin-top: var(--space-8);
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
