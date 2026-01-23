function openDeleteModal(id, name, action, controller) {
    document.getElementById("deleteItemName").innerText = name;

    const form = document.getElementById("deleteForm");
    form.action = `/${controller}/${action}/${id}`;

    const modal = new bootstrap.Modal(document.getElementById("deleteModal"));
    modal.show();
}
