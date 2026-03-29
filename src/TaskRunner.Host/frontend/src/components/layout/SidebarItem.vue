<script setup lang="ts">
import type { ToolDefinition } from '@/tools/registry';
import ToolIcon from '@/components/icons/ToolIcon.vue';

defineProps<{
  tool: ToolDefinition;
  active: boolean;
}>();
</script>

<template>
  <button
    class="sidebar-item"
    :class="{ active }"
    :title="tool.description"
  >
    <span class="sidebar-item-indicator"></span>
    <ToolIcon :name="tool.icon" :size="18" class="sidebar-item-icon" />
    <span class="sidebar-item-label">{{ tool.name }}</span>
  </button>
</template>

<style scoped>
.sidebar-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  width: 100%;
  padding: var(--space-2) var(--space-3);
  border-radius: var(--radius-md);
  color: var(--sidebar-text);
  position: relative;
  transition: color var(--duration-fast) var(--ease-out),
              background-color var(--duration-fast) var(--ease-out);
}

.sidebar-item:hover {
  color: var(--sidebar-text-hover);
  background: var(--sidebar-item-hover-bg);
}

.sidebar-item.active {
  color: var(--sidebar-item-active-text);
  background: var(--sidebar-item-active-bg);
}

.sidebar-item-indicator {
  position: absolute;
  left: -2px;
  top: 50%;
  transform: translateY(-50%);
  width: 3px;
  height: 0;
  border-radius: var(--radius-full);
  background: var(--sidebar-item-active-indicator);
  transition: height var(--duration-base) var(--ease-out);
}

.sidebar-item.active .sidebar-item-indicator {
  height: 20px;
}

.sidebar-item-icon {
  flex-shrink: 0;
  opacity: 0.7;
  transition: opacity var(--duration-fast) var(--ease-out);
}

.sidebar-item:hover .sidebar-item-icon,
.sidebar-item.active .sidebar-item-icon {
  opacity: 1;
}

.sidebar-item-label {
  font-size: var(--text-sm);
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
</style>
