<template>
    <div class="operation-panel">
        <h3>Synchronous Operations</h3>
        
        <div class="operation-types">
            <div v-for="op in operationTypes" :key="op.operationType" class="operation-card">
                <h4>{{ op.displayName }}</h4>
                
                <div v-if="op.operationType === 'calculator'" class="operation-form">
                    <div class="form-row">
                        <input 
                            type="number" 
                            v-model.number="calculatorParams.a" 
                            placeholder="A"
                            class="input-small"
                        />
                        <select v-model="calculatorParams.operation">
                            <option value="add">+</option>
                            <option value="subtract">-</option>
                            <option value="multiply">×</option>
                            <option value="divide">÷</option>
                        </select>
                        <input 
                            type="number" 
                            v-model.number="calculatorParams.b" 
                            placeholder="B"
                            class="input-small"
                        />
                    </div>
                    <button 
                        @click="executeCalculator" 
                        :disabled="loading"
                        class="btn btn-primary"
                    >
                        Calculate
                    </button>
                </div>
                
                <div v-else-if="op.operationType === 'system-info'" class="operation-form">
                    <button 
                        @click="executeSystemInfo" 
                        :disabled="loading"
                        class="btn btn-primary"
                    >
                        Get System Info
                    </button>
                </div>
            </div>
        </div>

        <div v-if="result" class="result-panel" :class="{ success: result.success, error: !result.success }">
            <h4>Result</h4>
            <p class="result-message">{{ result.message || result.error }}</p>
            <div v-if="result.data" class="result-data">
                <pre>{{ JSON.stringify(result.data, null, 2) }}</pre>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue';
import { api } from '../services/api';
import type { OperationTypeInfo, OperationResult } from '../types';

const operationTypes = ref<OperationTypeInfo[]>([]);
const loading = ref(false);
const result = ref<OperationResult | null>(null);

const calculatorParams = reactive({
    a: 10,
    b: 5,
    operation: 'add'
});

onMounted(async () => {
    operationTypes.value = await api.getOperationTypes();
});

async function executeCalculator(): Promise<void> {
    loading.value = true;
    result.value = null;
    try {
        result.value = await api.executeOperation('calculator', {
            a: calculatorParams.a,
            b: calculatorParams.b,
            operation: calculatorParams.operation
        });
    } catch (error) {
        result.value = { success: false, error: (error as Error).message, message: null, data: null };
    } finally {
        loading.value = false;
    }
}

async function executeSystemInfo(): Promise<void> {
    loading.value = true;
    result.value = null;
    try {
        result.value = await api.executeOperation('system-info', {});
    } catch (error) {
        result.value = { success: false, error: (error as Error).message, message: null, data: null };
    } finally {
        loading.value = false;
    }
}
</script>

<style scoped>
.operation-panel {
    background: white;
    border-radius: 8px;
    padding: 20px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.operation-panel h3 {
    margin-top: 0;
    margin-bottom: 16px;
    color: #333;
}

.operation-types {
    display: flex;
    flex-wrap: wrap;
    gap: 16px;
}

.operation-card {
    flex: 1;
    min-width: 250px;
    background: #f8f9fa;
    border: 1px solid #dee2e6;
    border-radius: 8px;
    padding: 16px;
}

.operation-card h4 {
    margin-top: 0;
    margin-bottom: 12px;
    color: #495057;
}

.operation-form {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.form-row {
    display: flex;
    gap: 8px;
    align-items: center;
}

.input-small {
    width: 80px;
    padding: 8px;
    border: 1px solid #ced4da;
    border-radius: 4px;
    font-size: 1em;
}

select {
    padding: 8px;
    border: 1px solid #ced4da;
    border-radius: 4px;
    font-size: 1em;
    background: white;
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
    background: #007bff;
    color: white;
}

.btn-primary:hover:not(:disabled) {
    background: #0056b3;
}

.btn:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}

.result-panel {
    margin-top: 20px;
    padding: 16px;
    border-radius: 8px;
}

.result-panel.success {
    background: #d4edda;
    border: 1px solid #c3e6cb;
}

.result-panel.error {
    background: #f8d7da;
    border: 1px solid #f5c6cb;
}

.result-panel h4 {
    margin-top: 0;
    margin-bottom: 8px;
}

.result-message {
    margin: 0 0 12px 0;
    font-weight: 500;
}

.result-data pre {
    background: rgba(255, 255, 255, 0.7);
    padding: 12px;
    border-radius: 4px;
    font-size: 0.85em;
    overflow-x: auto;
    margin: 0;
}
</style>
