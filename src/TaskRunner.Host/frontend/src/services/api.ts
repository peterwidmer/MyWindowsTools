import type {
    TaskTypeInfo,
    RunningTaskStatus,
    StartTaskResponse,
    OperationTypeInfo,
    OperationResult
} from '../types';

const API_BASE = '';

export const api = {
    // Task API
    async getTaskTypes(): Promise<TaskTypeInfo[]> {
        const response = await fetch(`${API_BASE}/api/tasks/types`);
        return response.json();
    },

    async getRunningTasks(): Promise<RunningTaskStatus[]> {
        const response = await fetch(`${API_BASE}/api/tasks/running`);
        return response.json();
    },

    async startTask(taskType: string, parameters: Record<string, unknown> = {}): Promise<StartTaskResponse> {
        const response = await fetch(`${API_BASE}/api/tasks/start`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ taskType, parameters })
        });
        return response.json();
    },

    async cancelTask(taskId: string): Promise<{ message?: string; error?: string }> {
        const response = await fetch(`${API_BASE}/api/tasks/${taskId}/cancel`, {
            method: 'POST'
        });
        return response.json();
    },

    // Operation API
    async getOperationTypes(): Promise<OperationTypeInfo[]> {
        const response = await fetch(`${API_BASE}/api/operations/types`);
        return response.json();
    },

    async executeOperation(operationType: string, parameters: Record<string, unknown> = {}): Promise<OperationResult> {
        const response = await fetch(`${API_BASE}/api/operations/execute`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ operationType, parameters })
        });
        return response.json();
    }
};
