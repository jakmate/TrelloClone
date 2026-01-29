// Store dotNetRef globally
let boardDotNetRef = null;

globalThis.initBoardsSortable = (gridId, dotNetRef) => {
  const grid = document.getElementById(gridId);
  if (!grid) return;

  if (grid.sortableInstance) {
    grid.sortableInstance.destroy();
  }

  grid.sortableInstance = new Sortable(grid, {
    animation: 150,
    ghostClass: "sortable-ghost",
    chosenClass: "sortable-chosen",
    dragClass: "sortable-drag",
    onEnd: (_evt) => {
      const items = grid.querySelectorAll("[data-id]");
      const orderedIds = Array.from(items).map((item) => item.dataset.id);
      dotNetRef.invokeMethodAsync("OnBoardReordered", orderedIds);
    },
  });
};

globalThis.initColumnsSortable = (containerId, dotNetRef) => {
  const container = document.getElementById(containerId);
  if (!container) return;

  // Store reference globally for tasks
  boardDotNetRef = dotNetRef;

  if (container.sortableInstance) {
    container.sortableInstance.destroy();
  }

  container.sortableInstance = new Sortable(container, {
    animation: 150,
    ghostClass: "sortable-ghost",
    chosenClass: "sortable-chosen",
    dragClass: "sortable-drag",
    handle: ".drag-handle",
    direction: "horizontal",
    onEnd: (_evt) => {
      const items = container.querySelectorAll("[data-column-id]");
      const orderedIds = Array.from(items).map((item) => item.dataset.columnId);
      dotNetRef.invokeMethodAsync("OnColumnsReordered", orderedIds);
    },
  });
};

globalThis.initTasksSortable = (containerId) => {
  const container = document.getElementById(containerId);
  if (!container || !boardDotNetRef) return;

  if (container.sortableInstance) {
    container.sortableInstance.destroy();
  }

  container.sortableInstance = new Sortable(container, {
    animation: 150,
    ghostClass: "sortable-ghost",
    chosenClass: "sortable-chosen",
    dragClass: "sortable-drag",
    handle: ".drag-handle",
    group: {
      name: "tasks",
      put: true,
    },
    onEnd: (evt) => {
      const taskId = evt.item.dataset.taskId;
      const fromColumnId = evt.from.dataset.columnId;
      const toColumnId = evt.to.dataset.columnId;
      const newIndex = evt.newIndex;

      if (fromColumnId === toColumnId) {
        const items = evt.to.querySelectorAll("[data-task-id]");
        const orderedIds = Array.from(items).map((item) => item.dataset.taskId);
        boardDotNetRef.invokeMethodAsync(
          "OnTasksReordered",
          toColumnId,
          orderedIds,
        );
      } else {
        boardDotNetRef.invokeMethodAsync(
          "OnTaskMovedBetweenColumns",
          taskId,
          fromColumnId,
          toColumnId,
          newIndex,
        );
      }
    },
  });
};
