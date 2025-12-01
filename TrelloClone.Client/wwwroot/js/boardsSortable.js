// Store dotNetRef globally
let boardDotNetRef = null;

window.initBoardsSortable = function (gridId, dotNetRef) {
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
    onEnd: function (evt) {
      const items = grid.querySelectorAll("[data-id]");
      const orderedIds = Array.from(items).map((item) =>
        item.getAttribute("data-id"),
      );
      dotNetRef.invokeMethodAsync("OnBoardReordered", orderedIds);
    },
  });
};

window.initColumnsSortable = function (containerId, dotNetRef) {
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
    onEnd: function (evt) {
      const items = container.querySelectorAll("[data-column-id]");
      const orderedIds = Array.from(items).map((item) =>
        item.getAttribute("data-column-id"),
      );
      dotNetRef.invokeMethodAsync("OnColumnsReordered", orderedIds);
    },
  });
};

window.initTasksSortable = function (containerId) {
  const container = document.getElementById(containerId);
  if (!container || !boardDotNetRef) return;

  const columnId = container.getAttribute("data-column-id");

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
    onEnd: function (evt) {
      const taskId = evt.item.getAttribute("data-task-id");
      const fromColumnId = evt.from.getAttribute("data-column-id");
      const toColumnId = evt.to.getAttribute("data-column-id");
      const newIndex = evt.newIndex;

      if (fromColumnId === toColumnId) {
        const items = evt.to.querySelectorAll("[data-task-id]");
        const orderedIds = Array.from(items).map((item) =>
          item.getAttribute("data-task-id"),
        );
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
