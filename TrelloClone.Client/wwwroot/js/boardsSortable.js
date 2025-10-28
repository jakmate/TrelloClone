window.initBoardsSortable = function (gridId, dotNetRef) {
    const grid = document.getElementById(gridId);
    if (!grid) return;

    // Destroy existing instance if any
    if (grid.sortableInstance) {
        grid.sortableInstance.destroy();
    }

    grid.sortableInstance = new Sortable(grid, {
        animation: 150,
        ghostClass: 'sortable-ghost',
        chosenClass: 'sortable-chosen',
        dragClass: 'sortable-drag',
        onEnd: function (evt) {
            const items = grid.querySelectorAll('[data-id]');
            const orderedIds = Array.from(items).map(item => item.getAttribute('data-id'));
            dotNetRef.invokeMethodAsync('OnBoardReordered', orderedIds);
        }
    });
};