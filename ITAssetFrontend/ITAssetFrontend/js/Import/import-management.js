/* =========================================================================
   IMPORT MANAGEMENT – import-management.js
   Mirrors pattern of hardware-management.js / software-management.js
   Follows SKILL_ADD_EDIT_DELETE §5 (Bulk Import) and CLAUDE.md standards
========================================================================= */

/* =========================================================================
   1. BIẾN TOÀN CỤC
========================================================================= */
const IMPORT_API = "http://localhost:5288/api";
const IMPORT_MAX_SIZE_BYTES = 5 * 1024 * 1024; // 5 MB
const IMPORT_ALLOWED_EXT = [".xlsx", ".xls", ".csv"];

// State cho từng tab
var importState = {
    hardware: { file: null, loading: false },
    software: { file: null, loading: false },
    license:  { file: null, loading: false }
};

/* =========================================================================
   2. KHỞI TẠO
========================================================================= */
window.initImportManagement = function () {
    switchTab("hardware");
};

/* =========================================================================
   3. ĐIỀU HƯỚNG TAB
========================================================================= */
window.switchTab = function (type) {
    // Ẩn tất cả panel
    ["hardware", "software", "license"].forEach(t => {
        const panel = document.getElementById("panel-" + t);
        const btn   = document.getElementById("tab-" + t);
        if (panel) panel.classList.remove("active");
        if (btn)   btn.classList.remove("active");
    });

    // Hiện panel được chọn
    const activePanel = document.getElementById("panel-" + type);
    const activeBtn   = document.getElementById("tab-" + type);
    if (activePanel) activePanel.classList.add("active");
    if (activeBtn)   activeBtn.classList.add("active");
};

/* =========================================================================
   4. XỬ LÝ CHỌN FILE (input + drag & drop)
========================================================================= */
window.handleFileSelect = function (event, type) {
    const file = event.target.files[0];
    if (file) applyFile(type, file);
};

window.handleDragOver = function (event, type) {
    event.preventDefault();
    const zone = document.getElementById("dropzone-" + type);
    if (zone) zone.classList.add("dragover");
};

window.handleDragLeave = function (event, type) {
    const zone = document.getElementById("dropzone-" + type);
    if (zone) zone.classList.remove("dragover");
};

window.handleDrop = function (event, type) {
    event.preventDefault();
    const zone = document.getElementById("dropzone-" + type);
    if (zone) zone.classList.remove("dragover");

    const file = event.dataTransfer.files[0];
    if (file) applyFile(type, file);
};

function applyFile(type, file) {
    // Validate extension
    const ext = "." + file.name.split(".").pop().toLowerCase();
    if (!IMPORT_ALLOWED_EXT.includes(ext)) {
        showImportAlert(`Invalid file format. Only ${IMPORT_ALLOWED_EXT.join(", ")} are accepted.`, "File Error", true);
        return;
    }

    // Validate size
    if (file.size > IMPORT_MAX_SIZE_BYTES) {
        showImportAlert("File size exceeds the 5 MB limit.", "File Too Large", true);
        return;
    }

    // Lưu state
    importState[type].file = file;

    // Hiện preview
    const preview  = document.getElementById("preview-" + type);
    const nameEl   = document.getElementById("fileName-" + type);
    const metaEl   = document.getElementById("fileMeta-" + type);
    const btnImport = document.getElementById("btnImport-" + type);

    if (preview)  preview.style.display = "block";
    if (nameEl)   nameEl.innerText  = file.name;
    if (metaEl)   metaEl.innerText  = `${formatFileSize(file.size)} · ${ext.toUpperCase()}`;
    if (btnImport) btnImport.disabled = false;

    // Reset kết quả cũ
    const resultEl = document.getElementById("result-" + type);
    if (resultEl) { resultEl.style.display = "none"; resultEl.innerHTML = ""; }
}

window.removeFile = function (type) {
    importState[type].file = null;

    const preview  = document.getElementById("preview-" + type);
    const fileInput = document.getElementById("file" + capitalize(type));
    const btnImport = document.getElementById("btnImport-" + type);

    if (preview)   preview.style.display = "none";
    if (fileInput) fileInput.value = "";
    if (btnImport) btnImport.disabled = true;

    const resultEl = document.getElementById("result-" + type);
    if (resultEl) { resultEl.style.display = "none"; resultEl.innerHTML = ""; }
};

/* =========================================================================
   5. SUBMIT IMPORT – GỌI API
   Theo SKILL_ADD_EDIT_DELETE §5:
   - Validate ALL rows trước khi insert
   - Nếu có lỗi → trả về file lỗi để download
   - Nếu không lỗi → insert trong 1 transaction và trả kết quả
========================================================================= */
window.submitImport = async function (type) {
    const state = importState[type];
    if (!state.file) {
        showImportAlert("Please select a file before importing.", "No File Selected", true);
        return;
    }

    if (state.loading) return; // Chống double-click

    state.loading = true;
    const btnImport = document.getElementById("btnImport-" + type);
    if (btnImport) { btnImport.disabled = true; btnImport.innerText = "Importing…"; }

    // Hiện progress modal
    showProgressModal(`Uploading and validating ${type} data…`);

    const formData = new FormData();
    formData.append("file", state.file);

    try {
        const token = localStorage.getItem("token");
        const res = await fetch(`${IMPORT_API}/import/${type}`, {
            method: "POST",
            headers: {
                ...(token ? { "Authorization": "Bearer " + token } : {})
            },
            body: formData
        });

        closeImportModal("importProgressModal");

        if (res.ok) {
            const contentType = res.headers.get("content-type") || "";

            if (contentType.includes("application/json")) {
                // THÀNH CÔNG: API trả JSON
                const result = await res.json();
                renderSuccessResult(type, result);
                removeFile(type); // Reset sau khi import xong
            } else {
                // CÓ LỖI: API trả file Excel lỗi để download
                const blob = await res.blob();
                const cd   = res.headers.get("content-disposition") || "";
                const fileNameMatch = cd.match(/filename[^;=\n]*=["']?([^"';\n]+)/i);
                const errorFileName = fileNameMatch
                    ? fileNameMatch[1]
                    : `${capitalize(type)}_Import_Errors.xlsx`;

                // Đếm tổng số hàng từ file gốc (dự phòng)
                renderErrorResult(type, blob, errorFileName, state.file.name);
            }
        } else {
            // HTTP error (400, 500…)
            let errMsg = "An error occurred during import.";
            try {
                const errJson = await res.json();
                errMsg = errJson.message || errMsg;
            } catch (_) {
                errMsg = await res.text() || errMsg;
            }
            renderHttpError(type, errMsg, res.status);
        }

    } catch (err) {
        closeImportModal("importProgressModal");
        renderHttpError(type, "Network error: " + err.message, 0);
        console.error("Import error:", err);
    } finally {
        state.loading = false;
        if (btnImport) {
            btnImport.disabled = !importState[type].file;
            btnImport.innerText = `↑ Import ${capitalize(type)}`;
        }
    }
};

/* =========================================================================
   6. RENDER KẾT QUẢ
========================================================================= */

// Thành công: JSON từ API
function renderSuccessResult(type, result) {
    const totalRows    = result.totalRows    || 0;
    const successCount = result.successCount || 0;
    const message      = result.message || "All records imported successfully.";

    // 1. Hiện success modal
    showImportSuccessModal(type, totalRows, successCount, message);

    // 2. Render result summary bên dưới nút Import (persistent)
    const resultEl = document.getElementById("result-" + type);
    if (!resultEl) return;

    resultEl.style.display = "block";
    resultEl.innerHTML = `
        <div class="result-header success">
            <span>✅ Import Completed Successfully</span>
        </div>
        <div class="result-stats">
            <div class="stat-item">
                <div class="stat-number total">${totalRows}</div>
                <div class="stat-label">Total Rows</div>
            </div>
            <div class="stat-item">
                <div class="stat-number success">${successCount}</div>
                <div class="stat-label">Imported</div>
            </div>
            <div class="stat-item">
                <div class="stat-number error">0</div>
                <div class="stat-label">Errors</div>
            </div>
        </div>
        <div class="result-actions">
            <span class="result-msg">${escapeHtml(message)}</span>
            <button class="btn-import-again" onclick="resetImport('${type}')">↑ Import Another File</button>
        </div>
    `;
}

// Hiện success modal với thông tin import
function showImportSuccessModal(type, totalRows, successCount, message) {
    const headlineEl = document.getElementById("importSuccessHeadline");
    const msgEl      = document.getElementById("importSuccessMsg");
    const totalEl    = document.getElementById("successStatTotal");
    const countEl    = document.getElementById("successStatCount");
    const titleEl    = document.getElementById("importSuccessTitle");
    const modal      = document.getElementById("importSuccessModal");

    if (!modal) return;

    if (titleEl)    titleEl.innerText    = `${capitalize(type)} Import Completed`;
    if (headlineEl) headlineEl.innerText = `${successCount} record${successCount !== 1 ? "s" : ""} imported successfully!`;
    if (msgEl)      msgEl.innerText      = message;
    if (totalEl)    totalEl.innerText    = totalRows;
    if (countEl)    countEl.innerText    = successCount;

    modal.style.display = "block";
}

// Có lỗi: API trả về file Excel lỗi → cho phép download
function renderErrorResult(type, blob, errorFileName, originalFileName) {
    const resultEl = document.getElementById("result-" + type);
    if (!resultEl) return;

    // Tạo object URL để download
    const url = URL.createObjectURL(blob);

    resultEl.style.display = "block";
    resultEl.innerHTML = `
        <div class="result-header error">
            <span>⚠️ Import Failed — Validation Errors Found</span>
        </div>
        <div class="result-stats">
            <div class="stat-item">
                <div class="stat-number total">—</div>
                <div class="stat-label">Total Rows</div>
            </div>
            <div class="stat-item">
                <div class="stat-number success">0</div>
                <div class="stat-label">Imported</div>
            </div>
            <div class="stat-item">
                <div class="stat-number error">!</div>
                <div class="stat-label">Has Errors</div>
            </div>
        </div>
        <div class="result-actions" style="flex-wrap: wrap; gap: 10px;">
            <p class="result-msg" style="width:100%; margin:0 0 8px 0; color:#c62828; font-weight:bold;">
                One or more rows failed validation. No data was imported.<br>
                <span style="font-weight:normal; color:#555;">Download the error file, fix the highlighted rows, and re-upload.</span>
            </p>
            <a id="downloadErrorLink-${type}" href="${url}" download="${errorFileName}">
                <button class="btn-download-err" onclick="scheduleRevokeUrl('${type}')">
                    ⬇ Download Error File
                </button>
            </a>
            <button class="btn-import-again" onclick="resetImport('${type}')">↑ Upload Fixed File</button>
        </div>
    `;

    // Lưu URL để revoke sau
    resultEl.dataset.blobUrl = url;
}

// HTTP error (400 / 500)
function renderHttpError(type, message, status) {
    const resultEl = document.getElementById("result-" + type);
    if (!resultEl) return;

    resultEl.style.display = "block";
    resultEl.innerHTML = `
        <div class="result-header error">
            <span>❌ Import Error${status ? " (HTTP " + status + ")" : ""}</span>
        </div>
        <div class="result-actions">
            <span class="result-msg" style="color:#b71c1c;">${escapeHtml(message)}</span>
            <button class="btn-import-again" onclick="resetImport('${type}')">Try Again</button>
        </div>
    `;
}

/* =========================================================================
   7. DOWNLOAD TEMPLATE
========================================================================= */
window.downloadTemplate = function (type) {
    const token = localStorage.getItem("token");
    const url   = `${IMPORT_API}/import/template/${type}`;

    // Dùng hidden <a> để trigger download
    const link = document.createElement("a");
    link.href  = url;
    // Nếu có token, mở qua fetch rồi blob
    if (token) {
        showProgressModal("Preparing template…");
        fetch(url, { headers: { "Authorization": "Bearer " + token } })
            .then(res => {
                if (!res.ok) throw new Error("Template download failed.");
                return res.blob();
            })
            .then(blob => {
                closeImportModal("importProgressModal");
                const blobUrl = URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = blobUrl;
                a.download = `${capitalize(type)}_Import_Template.xlsx`;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                setTimeout(() => URL.revokeObjectURL(blobUrl), 3000);
            })
            .catch(err => {
                closeImportModal("importProgressModal");
                showImportAlert("Template download failed: " + err.message, "Error", true);
            });
    } else {
        // Fallback: truy cập thẳng URL
        link.download = `${capitalize(type)}_Import_Template.xlsx`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
};

/* =========================================================================
   8. RESET / CLEAN UP
========================================================================= */
window.resetImport = function (type) {
    // Revoke blob URL nếu có
    const resultEl = document.getElementById("result-" + type);
    if (resultEl && resultEl.dataset.blobUrl) {
        URL.revokeObjectURL(resultEl.dataset.blobUrl);
        delete resultEl.dataset.blobUrl;
    }

    removeFile(type);
};

// Revoke object URL sau khi người dùng click download (3 giây)
window.scheduleRevokeUrl = function (type) {
    const resultEl = document.getElementById("result-" + type);
    if (resultEl && resultEl.dataset.blobUrl) {
        setTimeout(() => {
            URL.revokeObjectURL(resultEl.dataset.blobUrl);
        }, 5000);
    }
};

/* =========================================================================
   9. MODAL UTILITIES
========================================================= */
function showProgressModal(msg) {
    const msgEl   = document.getElementById("importProgressMsg");
    const titleEl = document.getElementById("importModalTitle");
    const modal   = document.getElementById("importProgressModal");
    if (msgEl)   msgEl.innerText   = msg;
    if (titleEl) titleEl.innerText = "Processing…";
    if (modal)   modal.style.display = "block";
}

window.closeImportModal = function (modalId) {
    const el = document.getElementById(modalId);
    if (el) el.style.display = "none";
};

window.showImportAlert = function (message, title = "Notification", isError = false) {
    const msgEl    = document.getElementById("importAlertMsg");
    const titleEl  = document.getElementById("importAlertTitle");
    const headerEl = document.getElementById("importAlertHeader");
    const modal    = document.getElementById("importAlertModal");

    if (!msgEl) { alert(`${title}: ${message}`); return; }

    msgEl.innerText   = message;
    if (titleEl)  titleEl.innerText  = title;
    if (headerEl) headerEl.style.backgroundColor = isError ? "#d32f2f" : "#000066";
    if (modal)    modal.style.display = "block";
};

/* =========================================================================
   10. TIỆN ÍCH
========================================================================= */
function formatFileSize(bytes) {
    if (bytes < 1024)       return bytes + " B";
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + " KB";
    return (bytes / (1024 * 1024)).toFixed(2) + " MB";
}

function capitalize(str) {
    return str.charAt(0).toUpperCase() + str.slice(1);
}

function escapeHtml(str) {
    return String(str)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
}