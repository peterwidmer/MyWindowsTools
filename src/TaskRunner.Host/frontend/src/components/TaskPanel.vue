<template>
    <div class="task-panel">
        <h3>Long-Running Tasks</h3>
        
        <div class="task-starters">
            <div v-for="taskType in taskTypes" :key="taskType.taskType" class="task-starter">
                <h4>{{ taskType.displayName }}</h4>
                
                <div v-if="taskType.taskType === 'file-processing'" class="task-form">
                    <div class="form-group">
                        <label>Number of files:</label>
                        <input 
                            type="number" 
                            v-model.number="fileProcessingParams.fileCount" 
                            min="1" 
                            max="100"
                        />
                    </div>
                    <button 
                        @click="startFileProcessing" 
                        :disabled="isStarting"
                        class="btn btn-primary"
                    >
                        Start File Processing
                    </button>
                </div>
                
                <div v-else-if="taskType.taskType === 'data-sync'" class="task-form">
                    <div class="form-group">
                        <label>Number of records:</label>
                        <input 
                            type="number" 
                            v-model.number="dataSyncParams.recordCount" 
                            min="10" 
                            max="500"
                        />
                    </div>
                    <button 
                        @click="startDataSync" 
                        :disabled="isStarting"
                        class="btn btn-primary"
                    >
                        Start Data Sync
                    </button>
                </div>
            </div>
        </div>

        <div v-if="tasks.length > 0" class="active-tasks">
            <h3>Task Progress</h3>
            <TaskProgress 
                v-for="task in tasks" 
                :key="task.taskId" 
                :task="task"
                @cancel="cancelTask"
            />
        </div>

        <div v-else class="no-tasks">
            <p>No tasks running. Click a button above to start a task.</p>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, reactive } from 'vue';
import { api } from '../services/api';
import { taskHub } from '../services/taskHub';
import TaskProgress from './TaskProgress.vue';
import type { 
    TaskTypeInfo, 
    TaskState,
    TaskStartedEvent,
    TaskProgressEvent,
    TaskCompletedEvent,
    TaskCancelledEvent,
    TaskFailedEvent
} from '../types';

const taskTypes = ref<TaskTypeInfo[]>([]);
const tasks = ref<TaskState[]>([]);
const isStarting = ref(false);

const fileProcessingParams = reactive({
    fileCount: 10
});

const dataSyncParams = reactive({
    recordCount: 50
});

onMounted(async () => {
    // Load available task types
    taskTypes.value = await api.getTaskTypes();

    // Connect to SignalR hub
    await taskHub.connect();

    // Set up event listeners
    taskHub.on('TaskStarted', handleTaskStarted);
    taskHub.on('TaskProgress', handleTaskProgress);
    taskHub.on('TaskCompleted', handleTaskCompleted);
    taskHub.on('TaskCancelled', handleTaskCancelled);
    taskHub.on('TaskFailed', handleTaskFailed);
});

onUnmounted(() => {
    taskHub.off('TaskStarted', handleTaskStarted);
    taskHub.off('TaskProgress', handleTaskProgress);
    taskHub.off('TaskCompleted', handleTaskCompleted);
    taskHub.off('TaskCancelled', handleTaskCancelled);
    taskHub.off('TaskFailed', handleTaskFailed);
});

function findTask(taskId: string): TaskState | undefined {
    return tasks.value.find(t => t.taskId === taskId);
}

function handleTaskStarted(data: TaskStartedEvent): void {
    const task = findTask(data.taskId);
    if (task) {
        task.status = 'running';
        task.startTime = data.startTime;
    }
}

function handleTaskProgress(data: TaskProgressEvent): void {
    const task = findTask(data.taskId);
    if (task) {
        task.progress = data.progress;
    }
}

function handleTaskCompleted(data: TaskCompletedEvent): void {
    const task = findTask(data.taskId);
    if (task) {
        task.status = 'completed';
        task.result = data.result;
        task.endTime = data.endTime;
    }
}

function handleTaskCancelled(data: TaskCancelledEvent): void {
    const task = findTask(data.taskId);
    if (task) {
        task.status = 'cancelled';
        task.endTime = data.endTime;
    }
}

function handleTaskFailed(data: TaskFailedEvent): void {
    const task = findTask(data.taskId);
    if (task) {
        task.status = 'failed';
        task.result = { success: false, error: data.error, message: null, data: null };
        task.endTime = data.endTime;
    }
}

async function startTask(taskType: string, parameters: Record<string, unknown>): Promise<void> {
    isStarting.value = true;
    try {
        const response = await api.startTask(taskType, parameters);
        const taskId = response.taskId;

        // Add task to list
        tasks.value.unshift({
            taskId,
            taskType,
            status: 'running',
            progress: null,
            result: null
        });

        // Subscribe to task updates
        await taskHub.subscribeToTask(taskId);
    } catch (error) {
        console.error('Failed to start task:', error);
    } finally {
        isStarting.value = false;
    }
}

async function startFileProcessing(): Promise<void> {
    await startTask('file-processing', { fileCount: fileProcessingParams.fileCount });
}

async function startDataSync(): Promise<void> {
    await startTask('data-sync', { recordCount: dataSyncParams.recordCount });
}

async function cancelTask(taskId: string): Promise<void> {
    try {
        await api.cancelTask(taskId);
    } catch (error) {
        console.error('Failed to cancel task:', error);
    }
}
</script>

<style scoped>
.task-panel {
    background: white;
    border-radius: 8px;
    padding: 20px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.task-panel h3 {
    margin-top: 0;
    margin-bottom: 16px;
    color: #333;
}

.task-starters {
    display: flex;
    flex-wrap: wrap;
    gap: 16px;
    margin-bottom: 24px;
}

.task-starter {
    flex: 1;
    min-width: 250px;
    background: #f8f9fa;
    border: 1px solid #dee2e6;
    border-radius: 8px;
    padding: 16px;
}

.task-starter h4 {
    margin-top: 0;
    margin-bottom: 12px;
    color: #495057;
}

.task-form {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.form-group {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.form-group label {
    font-size: 0.9em;
    color: #6c757d;
}

.form-group input {
    padding: 8px;
    border: 1px solid #ced4da;
    border-radius: 4px;
    font-size: 1em;
}

.btn {
    padding: 10px 20px;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 1em;
    transition: background 0.2s;
}

.btn-primary {
    background: #28a745;
    color: white;
}

.btn-primary:hover:not(:disabled) {
    background: #218838;
}

.btn:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}

.active-tasks {
    margin-top: 24px;
    padding-top: 24px;
    border-top: 1px solid #dee2e6;
}

.no-tasks {
    margin-top: 24px;
    padding: 24px;
    text-align: center;
    background: #f8f9fa;
    border-radius: 8px;
    color: #6c757d;
}
</style>
