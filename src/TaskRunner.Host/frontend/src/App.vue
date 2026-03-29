<script setup lang="ts">
import { ref } from 'vue';
import TheSidebar from './components/layout/TheSidebar.vue';
import DirectorySizeTool from './components/tools/DirectorySizeTool.vue';
import FindFilesTool from './components/tools/FindFilesTool.vue';
import { tools } from './tools/registry';

const selectedToolId = ref(tools[0]?.id ?? '');
</script>

<template>
  <div class="app-shell">
    <TheSidebar
      :tools="tools"
      :selected-tool-id="selectedToolId"
      @select-tool="selectedToolId = $event"
    />
    <main class="app-content">
      <Transition name="fade-slide" mode="out-in">
        <DirectorySizeTool v-if="selectedToolId === 'directory-size'" key="directory-size" />
        <FindFilesTool v-else-if="selectedToolId === 'find-files'" key="find-files" />
        <div v-else key="empty" class="empty-state">
          <p>Select a tool from the sidebar to get started.</p>
        </div>
      </Transition>
    </main>
  </div>
</template>

<style scoped>
.app-shell {
  display: flex;
  height: 100%;
  width: 100%;
  overflow: hidden;
}

.app-content {
  flex: 1;
  overflow-y: auto;
  background: var(--content-bg);
}

.empty-state {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--text-muted);
  font-size: var(--text-lg);
}
</style>

