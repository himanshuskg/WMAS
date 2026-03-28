function openConfirmAction(options) {
    const {
        title,
        message,
        buttonText = "Confirm",
        controller,
        action,
        id,
        actionUrl,
        onConfirm
    } = options;

    // Update modal content
    document.getElementById("confirmActionTitle").innerText = title;
    document.getElementById("confirmActionMessage").innerHTML = message;
    document.getElementById("confirmActionButton").innerText = buttonText;

    const form = document.getElementById("confirmActionForm");

    // Reset previous state
    form.action = "";
    form.onsubmit = null;

    // Set POST target
    if (controller && action && id !== undefined) {
        form.action = `/${controller}/${action}/${id}`;
    } else if (actionUrl) {
        form.action = actionUrl;
    } else {
        console.warn("No action URL provided for confirmation modal");
    }

    // Handle custom callback or normal form submit
    if (onConfirm) {
        // Custom logic (like edit confirmation) → prevent normal submit
        form.onsubmit = function (e) {
            e.preventDefault();
            onConfirm();
            return false;
        };
    } else {
        // Normal POST → allow form to submit naturally
        // No onsubmit handler = default browser submit behavior
    }

    // Show modal
    const modalElement = document.getElementById("confirmActionModal");
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    modal.show();
}
/*
 * Attaches change - tracking to a form to show a diff modal before submit.
 */
function attachEditConfirmation(options) {
    const {
        formId,
        fields,
        title = "Confirm Update",
        buttonText = "Confirm"
    } = options;

    const form = document.getElementById(formId);
    if (!form) return;

    form.addEventListener("submit", function (e) {
        e.preventDefault();

        const diffs = [];

        // 1. Deep Sanitize: Removes extra spaces, newlines, and "(Inactive)" tags for comparison only
        const sanitizeForCompare = (val) => {
            if (!val) return "";
            return val.toString()
                .replace(/\(inactive\)/gi, '') // Remove "(Inactive)" case-insensitive
                .replace(/\s+/g, ' ')          // Collapse multiple spaces to one
                .trim()
                .toLowerCase();
        };

        // 2. Format for Display: Keeps the string readable but removes extra whitespace
        const formatForDisplay = (val) => {
            if (!val || val.toString().trim() === "") return "Not Set";
            return val.toString().replace(/\s+/g, ' ').trim();
        };

        fields.forEach(f => {
            const input = form.querySelector(`[name="${f.name}"]`);
            if (!input) return;

            const rawOldValue = form.dataset[f.oldKey] || "";
            let displayOldValue = formatForDisplay(rawOldValue);
            let displayNewValue = "";
            let isChanged = false;

            if (input.tagName === "SELECT") {
                const selectedOption = input.options[input.selectedIndex];
                const newText = selectedOption ? selectedOption.text : "";

                displayNewValue = formatForDisplay(newText);

                // Compare logic: strip "(Inactive)" from both sides to see if the CORE value is different
                const cleanOld = sanitizeForCompare(rawOldValue);
                const cleanNew = sanitizeForCompare(newText);

                isChanged = cleanOld !== cleanNew;
            }
            else if (input.type === "checkbox") {
                const oldBool = sanitizeForCompare(rawOldValue) === "true";
                const newBool = input.checked;

                displayOldValue = oldBool ? "Yes" : "No";
                displayNewValue = newBool ? "Yes" : "No";
                isChanged = oldBool !== newBool;
            }
            else {
                const newValue = input.value || "";
                displayNewValue = formatForDisplay(newValue);
                isChanged = sanitizeForCompare(rawOldValue) !== sanitizeForCompare(newValue);
            }

            if (isChanged) {
                diffs.push(`
                    <li class="mb-2 d-flex align-items-start">
                        <span class="me-2"><strong>${f.label}:</strong></span>
                        <div class="flex-grow-1">
                            <span class="text-muted text-decoration-line-through small">${displayOldValue}</span>
                            <span class="mx-2 text-dark">&rarr;</span>
                            <span class="text-primary fw-bold">${displayNewValue}</span>
                        </div>
                    </li>`);
            }
        });

        if (diffs.length === 0) {
            form.submit();
            return;
        }

        openConfirmAction({
            title,
            buttonText,
            message: `
                <p class="mb-3 text-secondary border-bottom pb-2">The following updates were detected:</p>
                <ul class="list-unstyled border-start border-primary border-3 ps-3">
                    ${diffs.join("")}
                </ul>
                <div class="alert alert-warning mt-3 mb-0 py-2 small border-0 shadow-sm">
                    <i class="bi bi-exclamation-triangle-fill me-1"></i> 
                    Confirming will update the record with the selected values.
                </div>
            `,
            onConfirm: () => form.submit()
        });
    });
}