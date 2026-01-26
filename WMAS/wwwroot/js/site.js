function openConfirmModal(options) {
    document.getElementById("confirmActionTitle").innerText = options.title;
    document.getElementById("confirmActionMessage").innerText = options.message;
    document.getElementById("confirmActionButton").innerText = options.buttonText;

    const form = document.getElementById("confirmActionForm");
    form.action = `/${options.controller}/${options.action}/${options.id}`;

    const modal = new bootstrap.Modal(
        document.getElementById("confirmActionModal")
    );

    modal.show();
}
