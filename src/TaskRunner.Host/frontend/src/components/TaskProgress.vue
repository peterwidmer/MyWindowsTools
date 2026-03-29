<template>
    <div class="task-progress">
        <div class="task-header">
            <div class="task-info">
                <span class="task-type">{{ task.taskType }}</span>
                <span class="task-id">ID: {{ task.taskId.substring(0, 8) }}...</span>
            </div>
            <div class="task-status" :class="statusClass">{{ statusText }}</div>
        </div>

        <div v-if="task.progress" class="progress-details">
            <div class="phase" v-if="task.progress.currentPhase">
                <strong>Phase:</strong> {{ task.progress.currentPhase }}
            </div>
            <div class="message" v-if="task.progress.statusMessage">
                {{ task.progress.statusMessage }}
            </div>

            <div class="progress-bar-container" v-if="task.progress.percentComplete !== null">
                <div class="progress-bar" :style="{ width: task.progress.percentComplete + '%' }"></div>
                <span class="progress-percent">{{ task.progress.percentComplete }}%</span>
            </div>

            <div class="items-progress" v-if="task.progress.totalItems">
                <span>Items: {{ task.progress.processedItems || 0 }} / {{ task.progress.totalItems }}</span>
            </div>

            <div class="current-item" v-if="task.progress.currentItem">
                <strong>Current:</strong> {{ task.progress.currentItem }}
            </div>

            <div class="eta" v-if="task.progress.estimatedSecondsRemaining">
                <strong>ETA:</strong> {{ formatTime(task.progress.estimatedSecondsRemaining) }}
            </div>

            <div class="custom-data" v-if="task.progress.customData && Object.keys(task.progress.customData).length">
                <strong>Details:</strong>
                <ul>
                    <li v-for="(value, key) in task.progress.customData" :key="key">
                        {{ key }}: {{ value }}
                    </li>
                </ul>
            </div>
        </div>

        <div v-if="task.result" class="result-section">
            <div class="result-message" :class="{ success: task.result.success, error: !task.result.success }">
                {{ task.result.message || task.result.error }}
            </div>
            <div class="result-data" v-if="task.result.data">
                <pre>{{ JSON.stringify(task.result.data, null, 2) }}</pre>
            </div>
        </div>

        <div class="task-actions" v-if="task.status === 'running'">
            <button @click="$emit('cancel', task.taskId)" class="btn btn-cancel">Cancel</button>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { TaskState } from '../types';

const props = defineProps<{
    task: TaskState;
}>();

defineEmits<{
    cancel: [taskId: string];
}>();

const statusClass = computed(() => ({
    'status-running': props.task.status === 'running',
    'status-completed': props.task.status === 'completed',
    'status-cancelled': props.task.status === 'cancelled',
    'status-failed': props.task.status === 'failed'
}));

const statusText = computed(() => {
    switch (props.task.status) {
        case 'running': return 'Running';
        case 'completed': return 'Completed';
        case 'cancelled': return 'Cancelled';
        case 'failed': return 'Failed';
        default: return 'Unknown';
    }
});

function formatTime(seconds: number): string {
    if (seconds < 60) {
        return `${Math.round(seconds)}s`;
    }
    const mins = Math.floor(seconds / 60);
    const secs = Math.round(seconds % 60);
    return `${mins}m ${secs}s`;
}
</script>

<style scoped>
.task-progress {
    background: #f8f9fa;
    border: 1px solid #dee2e6;
    border-radius: 8px;
    padding: 16px;
    margin-bottom: 12px;
}

.task-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 12px;
}

.task-info {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.task-type {
    font-weight: 600;
    font-size: 1.1em;
}

.task-id {
    font-size: 0.85em;
    color: #6c757d;
    font-family: monospace;
}

.task-status {
    padding: 4px 12px;
    border-radius: 12px;
    font-size: 0.85em;
    font-weight: 500;
}

.status-running {
    background: #cce5ff;
    color: #004085;
}

.status-completed {
    background: #d4edda;
    color: #155724;
}

.status-cancelled {
    background: #fff3cd;
    color: #856404;
}

.status-failed {
    background: #f8d7da;
    color: #721c24;
}

.progress-details {
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.phase {
    color: #495057;
}

.message {
    color: #6c757d;
    font-style: italic;
}

.progress-bar-container {
    height: 24px;
    background: #e9ecef;
    border-radius: 4px;
    position: relative;
    overflow: hidden;
}

.progress-bar {
    height: 100%;
    background: linear-gradient(90deg, #007bff, #0056b3);
    transition: width 0.3s ease;
}

.progress-percent {
    position: absolute;
    left: 50%;
    top: 50%;
    transform: translate(-50%, -50%);
    font-weight: 500;
    font-size: 0.85em;
    color: #333;
}

.items-progress,
.current-item,
.eta {
    font-size: 0.9em;
    color: #495057;
}

.custom-data {
    font-size: 0.85em;
    background: #fff;
    padding: 8px;
    border-radius: 4px;
}

.custom-data ul {
    margin: 4px 0 0 0;
    padding-left: 20px;
}

.result-section {
    margin-top: 12px;
    padding-top: 12px;
    border-top: 1px solid #dee2e6;
}

.result-message {
    padding: 8px 12px;
    border-radius: 4px;
    margin-bottom: 8px;
}

.result-message.success {
    background: #d4edda;
    color: #155724;
}

.result-message.error {
    background: #f8d7da;
    color: #721c24;
}

.result-data pre {
    background: #fff;
    padding: 12px;
    border-radius: 4px;
    font-size: 0.85em;
    overflow-x: auto;
    margin: 0;
}

.task-actions {
    margin-top: 12px;
    display: flex;
    gap: 8px;
}

.btn {
    padding: 6px 16px;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.9em;
    transition: background 0.2s;
}

.btn-cancel {
    background: #dc3545;
    color: white;
}

.btn-cancel:hover {
    background: #c82333;
}
</style>
