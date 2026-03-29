<script setup lang="ts">
import type { ToolDefinition } from '@/tools/registry';
import SidebarItem from './SidebarItem.vue';

defineProps<{
  tools: ToolDefinition[];
  selectedToolId: string;
}>();

defineEmits<{
  selectTool: [id: string];
}>();
</script>

<template>
  <aside class="sidebar">
    <div class="sidebar-header">
      <div class="sidebar-brand">
        <div class="sidebar-logo">
          <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24" fill="none"
            stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z" />
          </svg>
        </div>
        <span class="sidebar-app-name">Windows Tools</span>
      </div>
    </div>

    <div class="sidebar-label">Tools</div>

    <nav class="sidebar-nav">
      <SidebarItem
        v-for="tool in tools"
        :key="tool.id"
        :tool="tool"
        :active="tool.id === selectedToolId"
        @click="$emit('selectTool', tool.id)"
      />
    </nav>

    <div class="sidebar-footer">
      <span class="sidebar-version">v1.0.0</span>
    </div>
  </aside>
</template>

<style scoped>
.sidebar {
  width: var(--sidebar-width);
  height: 100vh;
  background: var(--sidebar-bg);
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
  user-select: none;
}

.sidebar-header {
  padding: var(--space-5) var(--space-5) var(--space-4);
}

.sidebar-brand {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.sidebar-logo {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: var(--radius-md);
  background: linear-gradient(135deg, var(--accent), #8b5cf6);
  color: white;
  flex-shrink: 0;
}

.sidebar-app-name {
  font-size: var(--text-lg);
  font-weight: 700;
  color: var(--text-inverse);
  letter-spacing: -0.01em;
}

.sidebar-label {
  padding: var(--space-2) var(--space-5);
  font-size: var(--text-xs);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--sidebar-text);
  opacity: 0.5;
}

.sidebar-nav {
  flex: 1;
  overflow-y: auto;
  padding: var(--space-1) var(--space-3);
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.sidebar-footer {
  padding: var(--space-4) var(--space-5);
  border-top: 1px solid var(--sidebar-divider);
}

.sidebar-version {
  font-size: var(--text-xs);
  color: var(--sidebar-text);
  opacity: 0.4;
}
</style>
