/* =========================================================================
   1. KHỞI TẠO VÀ BIẾN TOÀN CỤC
========================================================================= */
var softwareData   = [];
var swTotal        = 0;
var swCurrentPage  = 1;
var swPageSize     = 10;

// Lưu software đang xem chi tiết (dùng cho nút Change/Uninstall trong detail modal)
var _currentDetailSoftware = null;

// Local permission cache
var _swPerm = window.currentPermission || {
    canView: true, canRequest: false, canApprove: false, canAdmin: false
};

window.initSoftwareManagement = function () {
    // Re-read permission cache
    _swPerm = window.currentPermission || _swPerm;
    _applySwPermission();
    loadSoftwareManagementData();
    setupDropdownBehavior();
    loadUserOptions();
    setupInstallAutocomplete();
    setupCopyAutocomplete();
};

// Called by permission-guard.js after it finishes loading
window.onPermissionLoaded = function(perm) {
    _swPerm = perm;
    _applySwPermission();
};

function _applySwPermission() {
    // "+ Install New Software" button
    const installBtns = document.querySelectorAll("[onclick='openInstallSoftwareModal()']");
    installBtns.forEach(function(el) {
        el.style.display = _swPerm.canRequest ? "" : "none";
    });
    // data-perm elements
    document.querySelectorAll("[data-perm='can_request']").forEach(function(el) {
        el.style.display = _swPerm.canRequest ? "" : "none";
    });
    // Permission banner
    if (typeof window.applyPermissionToUI === "function") {
        window.applyPermissionToUI();
    }
}

/* =========================================================================
   2. TẢI DỮ LIỆU CHÍNH (TÌM KIẾM & DANH SÁCH)
   API: POST /software/search
   DTO: { AssetControlNumber, SoftwareName, SoftwareVersion }
========================================================================= */
async function loadSoftwareManagementData() {
    const listContainer = document.getElementById("softwareList");
    if (!listContainer) return;

    listContainer.innerHTML = "<div style='padding: 20px; text-align: center;'>Loading software data...</div>";

    const payload = {
        AssetControlNumber: document.getElementById("filterSwControlNumber")?.value.trim() || "",
        SoftwareName:       document.getElementById("filterSwName")?.value.trim()          || "",
        SoftwareVersion:    document.getElementById("filterSwVersion")?.value.trim()       || ""
    };

    try {
        const response = await fetchAPI("/software/search", {
            method: "POST",
            body: JSON.stringify(payload)
        });

        softwareData  = Array.isArray(response) ? response : (response.data || []);
        swTotal       = softwareData.length;
        swCurrentPage = 1;
        render();
    } catch (error) {
        listContainer.innerHTML = `<div style="color: red; padding: 20px;">Failed to load data: ${error.message}</div>`;
        console.error(error);
    }
}

window.searchSoftware = function () {
    swCurrentPage = 1;
    loadSoftwareManagementData();
};

window.clearSoftwareSearch = function () {
    ['filterSwControlNumber', 'filterSwName', 'filterSwVersion'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = '';
    });
};

/* =========================================================================
   3. RENDER DANH SÁCH & PHÂN TRANG
========================================================================= */
function render() {
    renderSoftwareList();
    renderSwPagination();
}

function renderSoftwareList() {
    const list = document.getElementById("softwareList");
    if (!list) return;

    if (!softwareData || softwareData.length === 0) {
        list.innerHTML = "<div style='padding: 20px; text-align:center; color:#555;'>No software found.</div>";
        return;
    }

    const start    = (swCurrentPage - 1) * swPageSize;
    const pageData = softwareData.slice(start, start + swPageSize);

    list.innerHTML = pageData.map(s => `
        <div class="software-card">
            <div class="card-header">
                <div class="card-header-left">
                    <input type="checkbox" class="sw-checkbox" value="${s.softwareId}">
                    <a onclick="viewDetail(${s.softwareId})">View details</a>
                </div>
                <div class="card-header-right">
                    ${s.licenseId ? '<span style="background:#28a745;color:white;padding:2px 7px;border-radius:4px;font-size:11px;font-weight:bold;">[LINK]</span>' : ''}

                    <div class="dropdown">
                        <button class="btn-menu" onclick="toggleSwDropdown(event, ${s.softwareId})">Menu ▼</button>
                        <div id="sw-dropdown-${s.softwareId}" class="dropdown-content">
                            <a onclick="handleMenuAction('change', ${s.softwareId})">▶ Change Application</a>
                            <a onclick="handleMenuAction('reqCopy', ${s.softwareId})">▶ Copy Request</a>
                            <a onclick="handleMenuAction('uninstall', ${s.softwareId})">▶ Uninstall</a>
                            <a onclick="handleMenuAction('groupDetails', ${s.softwareId})">▶ Group Details</a>
                            <a onclick="handleMenuAction('appHistory', ${s.softwareId})">▶ Application History</a>
                            <a onclick="viewDetail(${s.softwareId})">▶ View Details</a>
                            <a onclick="handleMenuAction('invHistory', ${s.softwareId})">▶ Inventory History</a>
                        </div>
                    </div>
                </div>
            </div>

            <div class="card-body">
                <div class="cell cell-label">Software ID</div>
                <div class="cell cell-value">${s.softwareId}</div>
                <div class="cell cell-label">Software Name</div>
                <div class="cell cell-value">${s.softwareName || "-"}</div>

                <div class="cell cell-label">Version</div>
                <div class="cell cell-value">${s.softwareVersion || "-"}</div>
                <div class="cell cell-label">Software Type</div>
                <div class="cell cell-value">${s.softwareType || s.licenseType || "-"}</div>

                <div class="cell cell-label">IT Asset Control Number</div>
                <div class="cell cell-value">${s.assetControlNumber || "<span style='color:#aaa;'>Not Installed</span>"}</div>
                <div class="cell cell-label">Asset Name</div>
                <div class="cell cell-value">${s.assetName || "-"}</div>

                <div class="cell cell-label">License ID</div>
                <div class="cell cell-value">${s.licenseId || "-"}</div>
                <div class="cell cell-label">Group ID</div>
                <div class="cell cell-value">${s.groupId || "-"}</div>
            </div>
        </div>
    `).join("");
}

function renderSwPagination() {
    const totalPages = Math.ceil(swTotal / swPageSize) || 1;
    let buttons = `
        <button onclick="changeSwPage(1)"><<</button>
        <button onclick="changeSwPage(${Math.max(1, swCurrentPage - 1)})"><</button>
    `;
    for (let i = 1; i <= totalPages; i++) {
        buttons += `<button class="${i === swCurrentPage ? 'active' : ''}" onclick="changeSwPage(${i})">${i}</button>`;
    }
    buttons += `
        <button onclick="changeSwPage(${Math.min(totalPages, swCurrentPage + 1)})">></button>
        <button onclick="changeSwPage(${totalPages})">>></button>
    `;

    const start = swTotal === 0 ? 0 : (swCurrentPage - 1) * swPageSize + 1;
    const end   = Math.min(swCurrentPage * swPageSize, swTotal);

    ['swPaginationTop',   'swPaginationBottom' ].forEach(id => { const el = document.getElementById(id); if (el) el.innerHTML = buttons; });
    ['swShowStartTop',    'swShowStartBottom'  ].forEach(id => { const el = document.getElementById(id); if (el) el.innerText = start; });
    ['swShowEndTop',      'swShowEndBottom'    ].forEach(id => { const el = document.getElementById(id); if (el) el.innerText = end; });
    ['swTotalTop',        'swTotalBottom'      ].forEach(id => { const el = document.getElementById(id); if (el) el.innerText = swTotal; });
}

window.changeSwPage = function (page) { swCurrentPage = page; render(); };
window.changeSwPageSize = function (size) {
    swPageSize = parseInt(size);
    swCurrentPage = 1;
    ['swPageSizeTop', 'swPageSizeBottom'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = size;
    });
    render();
};

/* =========================================================================
   4. DROPDOWN BEHAVIOR
========================================================================= */
window.toggleSwDropdown = function (event, id) {
    event.stopPropagation();
    const current = document.getElementById(`sw-dropdown-${id}`);
    document.querySelectorAll('.dropdown-content').forEach(el => {
        if (el !== current) el.classList.remove('show');
    });
    if (current) current.classList.toggle("show");
};

window.toggleBulkDropdown = function (event, btnElement) {
    event.stopPropagation();
    const dropdown = btnElement.nextElementSibling;
    document.querySelectorAll('.dropdown-content').forEach(el => {
        if (el !== dropdown) el.classList.remove('show');
    });
    if (dropdown) dropdown.classList.toggle("show");
};

function setupDropdownBehavior() {
    window.addEventListener("click", function (event) {
        if (!event.target.matches('.btn-menu') && !event.target.matches('.btn-outline')) {
            document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
        }
    });
}

/* =========================================================================
   5. MENU CÁ NHÂN (PER-CARD ACTIONS)
========================================================================= */
window.handleMenuAction = function (action, softwareId) {
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
    const s = softwareData.find(x => x.softwareId === softwareId);

    // Actions requiring canRequest permission
    const writeActions = ['change', 'reqCopy', 'uninstall'];
    if (writeActions.includes(action) && !_swPerm.canRequest) {
        showCustomAlert(
            "You do not have permission to create requests.\nContact your Manager.",
            "Permission Denied", true
        );
        return;
    }

    switch (action) {
        case 'change':
            openChangeApplicationModal(s);
            break;

        case 'reqCopy':
            openCopyRequestModal(s);
            break;

        case 'uninstall':
            openUninstallRequestModal(s);
            break;

        case 'groupDetails':
            openGroupDetailsModal(s);
            break;

        case 'appHistory':
            loadApplicationHistory(softwareId);
            break;

        case 'invHistory':
            loadInventoryHistory(softwareId);
            break;
    }
};

/* =========================================================================
   6. VIEW DETAILS
   API: GET /software/{id}
========================================================================= */
window.viewDetail = async function (id) {
    try {
        const s = await fetchAPI(`/software/${id}`);
        _currentDetailSoftware = s;

        const tbody = document.getElementById('swDetailTableBody');
        if (tbody) {
            const row = (label, value) =>
                `<tr>
                    <th style="padding:8px;border:1px solid #ccc;background:#f0f4f8;width:35%;font-weight:bold;">${label}</th>
                    <td style="padding:8px;border:1px solid #ccc;">${value ?? '-'}</td>
                 </tr>`;

            tbody.innerHTML =
                row('Software ID',          s.softwareId) +
                row('Software Name',        s.softwareName) +
                row('Version',              s.softwareVersion) +
                row('Software Type',        s.softwareType) +
                row('License Type',         s.licenseType) +
                row('License ID',           s.licenseId) +
                row('Vendor ID',            s.vendorId) +
                row('Asset Control Number', s.assetControlNumber || '<span style="color:#aaa">Not Installed</span>') +
                row('Asset Name',           s.assetName) +
                row('Department',           s.departmentName) +
                row('Group ID',             s.groupId) +
                row('Installed By',         s.installedByName) +
                row('Install Date',         s.installedDate ? new Date(s.installedDate).toLocaleDateString() : null) +
                row('Description',          s.description);
        }
        document.getElementById('swDetailModal').style.display = 'block';
    } catch (e) {
        showCustomAlert("Lỗi khi tải thông tin: " + e.message, "Lỗi Hệ Thống", true);
    }
};

// Nút Change và Uninstall trong Detail Modal
window.openChangeFromDetail = function () {
    if (!_swPerm.canRequest) {
        showCustomAlert("You do not have permission to create requests.", "Permission Denied", true);
        return;
    }
    closeModal('swDetailModal');
    if (_currentDetailSoftware) openChangeApplicationModal(_currentDetailSoftware);
};
window.openUninstallFromDetail = function () {
    if (!_swPerm.canRequest) {
        showCustomAlert("You do not have permission to uninstall software.", "Permission Denied", true);
        return;
    }
    closeModal('swDetailModal');
    if (_currentDetailSoftware) openUninstallRequestModal(_currentDetailSoftware);
};

/* =========================================================================
   7. CHANGE APPLICATION
   API: POST /software/{id}/change-application
   DTO: SoftwareChangeApplicationDto
========================================================================= */
function openChangeApplicationModal(s) {
    if (!s) return;
    document.getElementById('changeSoftwareId').value    = s.softwareId;
    document.getElementById('changeSwNameDisplay').innerText  = `${s.softwareName} v${s.softwareVersion || ''}`;
    document.getElementById('changeSwAssetDisplay').innerText = s.assetControlNumber || '(Not linked to asset)';

    // Điền giá trị cũ & lưu vào data-old để detect thay đổi
    const fields = [
        { id: 'chgSwName',        value: s.softwareName    },
        { id: 'chgSwVersion',     value: s.softwareVersion },
        { id: 'chgSwLicenseType', value: s.licenseType     },
        { id: 'chgSwType',        value: s.softwareType    },
        { id: 'chgSwDesc',        value: s.description     }
    ];
    fields.forEach(f => {
        const el = document.getElementById(f.id);
        if (el) { el.value = f.value || ''; el.dataset.old = f.value || ''; }
    });

    document.getElementById('reason_swchange').value = '';
    document.getElementById('changeRequestModal').style.display = 'block';
}

window.submitChangeRequest = async function () {
    const softwareId  = parseInt(document.getElementById('changeSoftwareId').value);
    const firstApp    = document.getElementById('firstApp_swchange').value;
    const isSecOn     = document.getElementById('secAppFlag_swchange').checked;
    const secApp      = document.getElementById('secApp_swchange').value;

    if (!firstApp) { showCustomAlert("Vui lòng chọn First Approver!", "Cảnh báo", true); return; }
    if (isSecOn && !secApp) { showCustomAlert("Vui lòng chọn Second Approver!", "Cảnh báo", true); return; }

    // Gom các field thay đổi — chỉ gửi field != rỗng hoặc != old value
    const inputs   = document.querySelectorAll('.chg-sw-input');
    let hasChange  = false;
    const payload  = { FirstApproverId: parseInt(firstApp) };

    if (isSecOn && secApp) payload.SecondApproverId = parseInt(secApp);

    const fieldMap = {
        'software_name':    'SoftwareName',
        'software_version': 'SoftwareVersion',
        'license_type':     'LicenseType',
        'software_type':    'SoftwareType',
        'description':      'Description'
    };

    inputs.forEach(el => {
        const fieldKey = el.dataset.field;
        const dtoKey   = fieldMap[fieldKey];
        if (!dtoKey) return;
        const newVal   = el.value.trim();
        const oldVal   = (el.dataset.old || '').trim();
        if (newVal !== oldVal && newVal !== '') {
            payload[dtoKey] = newVal;
            hasChange = true;
        }
    });

    if (!hasChange) { showCustomAlert("Bạn chưa thay đổi thông tin nào!", "Cảnh báo", true); return; }

    const ok = await showCustomConfirm("Gửi Change Application cho phần mềm này?", "Xác nhận");
    if (!ok) return;

    try {
        await fetchAPI(`/software/${softwareId}/change-application`, {
            method: "POST",
            body:   JSON.stringify(payload)
        });
        showCustomAlert("Change Application đã được gửi thành công! Đang chờ phê duyệt.", "Thành công");
        closeModal('changeRequestModal');
    } catch (e) {
        showCustomAlert("Lỗi: " + e.message, "Lỗi Hệ Thống", true);
    }
};

/* =========================================================================
   8. COPY REQUEST
   API: POST /software/{id}/copy-request
   DTO: SoftwareCopyRequestDto
========================================================================= */
function openCopyRequestModal(s) {
    if (!s) return;
    document.getElementById('copySoftwareId').value           = s.softwareId;
    document.getElementById('copySwNameDisplay').innerText    = `${s.softwareName} v${s.softwareVersion || ''}`;
    document.getElementById('copySwAssetDisplay').innerText   = s.assetControlNumber || '(Not linked to asset)';
    document.getElementById('copyTargetAsset').value          = '';
    document.getElementById('copyDesc').value                 = '';
    document.getElementById('copyHwSuggestions').style.display = 'none';
    document.getElementById('copyRequestModal').style.display = 'block';
}

window.submitCopyRequest = async function () {
    const softwareId = parseInt(document.getElementById('copySoftwareId').value);
    const target     = document.getElementById('copyTargetAsset').value.trim();
    const firstApp   = document.getElementById('firstApp_copy').value;
    const isSecOn    = document.getElementById('secAppFlag_copy').checked;
    const secApp     = document.getElementById('secApp_copy').value;

    if (!target)   { showCustomAlert("Vui lòng nhập Target Asset Control Number!", "Cảnh báo", true); return; }
    if (!firstApp) { showCustomAlert("Vui lòng chọn First Approver!", "Cảnh báo", true); return; }
    if (isSecOn && !secApp) { showCustomAlert("Vui lòng chọn Second Approver!", "Cảnh báo", true); return; }

    const ok = await showCustomConfirm(`Copy phần mềm lên thiết bị [${target}]?`, "Xác nhận");
    if (!ok) return;

    const payload = {
        TargetAssetControlNumber: target,
        Description:              document.getElementById('copyDesc').value.trim() || null,
        FirstApproverId:          parseInt(firstApp),
        SecondApproverId:         isSecOn && secApp ? parseInt(secApp) : null
    };

    try {
        await fetchAPI(`/software/${softwareId}/copy-request`, {
            method: "POST",
            body:   JSON.stringify(payload)
        });
        showCustomAlert("Copy Request đã được gửi! Đang chờ phê duyệt.", "Thành công");
        closeModal('copyRequestModal');
    } catch (e) {
        showCustomAlert("Lỗi: " + e.message, "Lỗi Hệ Thống", true);
    }
};

/* =========================================================================
   9. UNINSTALL SOFTWARE (直接削除 – confirm before DELETE /software/{id})
   No approval workflow. User confirms, then the record is hard-deleted.
========================================================================= */
function openUninstallRequestModal(s) {
    if (!s) return;
    document.getElementById('uninstallSoftwareId').value         = s.softwareId;
    document.getElementById('uninstallSwNameDisplay').innerText  = `${s.softwareName} v${s.softwareVersion || ''}`;
    document.getElementById('uninstallSwAssetDisplay').innerText = s.assetControlNumber || '(Not linked to asset)';
    document.getElementById('uninstallConfirmModal').style.display = 'block';
}

window.executeUninstall = async function () {
    const softwareId = parseInt(document.getElementById('uninstallSoftwareId').value);
    const btnConfirm = document.getElementById('btnConfirmUninstall');

    if (!softwareId) return;

    // Disable button to prevent double-click
    if (btnConfirm) { btnConfirm.disabled = true; btnConfirm.innerText = 'Uninstalling...'; }

    try {
        await fetchAPI(`/software/${softwareId}`, { method: 'DELETE' });
        closeModal('uninstallConfirmModal');
        showCustomAlert('Software has been uninstalled successfully.', 'Success');
        loadSoftwareManagementData();
    } catch (e) {
        showCustomAlert('Uninstall failed: ' + e.message, 'Error', true);
    } finally {
        if (btnConfirm) { btnConfirm.disabled = false; btnConfirm.innerText = 'Yes, Uninstall'; }
    }
};

/* =========================================================================
   10. GROUP DETAILS
   API: GET /software/group/{groupId}
========================================================================= */
function openGroupDetailsModal(s) {
    document.getElementById('groupDetSwId').innerText    = s.softwareId;
    document.getElementById('groupDetGroupId').innerText = s.groupId || "Not in any group";

    const container = document.getElementById('groupMembersContainer');
    if (container) container.innerHTML = '';

    if (s.groupId) {
        fetchAPI(`/software/group/${s.groupId}`)
            .then(res => {
                if (!container) return;
                if (!res || !res.members || res.members.length === 0) {
                    container.innerHTML = '<p style="color:#000000; font-size:13px;">No members found.</p>';
                    return;
                }
                container.innerHTML = `
                    <p style="font-size:13px; font-weight:bold; color:#000066; margin-bottom:8px;">
                        Group members (${res.count}):
                    </p>
                    <table style="width:100%;border-collapse:collapse;font-size:12px;">
                        <thead><tr style="background:#000066;color:white;">
                            <th style="padding:6px;border:1px solid #000000;color:black;">ID</th>
                            <th style="padding:6px;border:1px solid #000000;color:black;">Name</th>
                            <th style="padding:6px;border:1px solid #000000;color:black;">Version</th>
                            <th style="padding:6px;border:1px solid #000000;color:black;">Asset</th>
                        </tr></thead>
                        <tbody>
                            ${res.members.map(m => `
                                <tr>
                                    <td style="padding:6px;border:1px solid #000000;">${m.softwareId}</td>
                                    <td style="padding:6px;border:1px solid #000000;">${m.softwareName}</td>
                                    <td style="padding:6px;border:1px solid #000000;">${m.softwareVersion || '-'}</td>
                                    <td style="padding:6px;border:1px solid #000000;">${m.assetControlNumber || '-'}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>`;
            })
            .catch(() => {
                if (container) container.innerHTML = '<p style="color:#d32f2f;font-size:13px;">Failed to load group members.</p>';
            });
    }
    document.getElementById('groupDetailsModal').style.display = 'block';
}

/* =========================================================================
   11. APPLICATION HISTORY
   API: GET /software/{id}/application-history
========================================================================= */
window.loadApplicationHistory = async function (softwareId) {
    try {
        const data = await fetchAPI(`/software/${softwareId}/application-history`);
        document.getElementById("historyModalTitle").innerText = `Application History – Software ID: ${softwareId}`;

        document.getElementById("historyTableHead").innerHTML = `
            <tr>
                <th style="padding:8px;border:1px solid #555;">App ID</th>
                <th style="padding:8px;border:1px solid #555;">Applicant</th>
                <th style="padding:8px;border:1px solid #555;">Date</th>
                <th style="padding:8px;border:1px solid #555;">Type</th>
                <th style="padding:8px;border:1px solid #555;">Status</th>
                <th style="padding:8px;border:1px solid #555;">Description</th>
            </tr>`;

        const tbody = document.getElementById("historyTableBody");
        if (!data || data.length === 0) {
            tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;padding:12px;">No application history found.</td></tr>`;
        } else {
            tbody.innerHTML = data.map(item => `
                <tr>
                    <td style="padding:8px;border:1px solid #ddd;">${item.applicationId}</td>
                    <td style="padding:8px;border:1px solid #ddd;">${item.applicant || '-'}</td>
                    <td style="padding:8px;border:1px solid #ddd;">${new Date(item.applicationDate).toLocaleDateString()}</td>
                    <td style="padding:8px;border:1px solid #ddd;">${item.applicationType || '-'}</td>
                    <td style="padding:8px;border:1px solid #ddd;">${item.status || '-'}</td>
                    <td style="padding:8px;border:1px solid #ddd;">${item.description || '-'}</td>
                </tr>`).join('');
        }
        document.getElementById("historyModal").style.display = "block";
    } catch (e) {
        showCustomAlert("Lỗi tải lịch sử: " + e.message, "Lỗi Hệ Thống", true);
    }
};

/* =========================================================================
   12. INVENTORY HISTORY
   API: GET /software/{id}/inventory-history
========================================================================= */
window.loadInventoryHistory = async function (softwareId) {
    try {
        const res = await fetchAPI(`/software/${softwareId}/inventory-history`);
        document.getElementById("historyModalTitle").innerText = `Inventory History – Software ID: ${softwareId}`;

        document.getElementById("historyTableHead").innerHTML = `
            <tr>
                <th style="padding:8px;border:1px solid #555;">App ID</th>
                <th style="padding:8px;border:1px solid #555;">Applicant</th>
                <th style="padding:8px;border:1px solid #555;">Date</th>
                <th style="padding:8px;border:1px solid #555;">Type</th>
                <th style="padding:8px;border:1px solid #555;">Description</th>
            </tr>`;

        const tbody   = document.getElementById("historyTableBody");
        const history = res.inventoryHistory || [];

        if (history.length === 0) {
            tbody.innerHTML = `<tr><td colspan="5" style="text-align:center;padding:12px;">No inventory history found.</td></tr>`;
        } else {
            tbody.innerHTML = history.map(item => `
                <tr>
                    <td style="padding:8px;border:1px solid #ddd;">${item.applicationId}</td>
                    <td style="padding:8px;border:1px solid #ddd;">${item.applicant || '-'}</td>
                    <td style="padding:8px;border:1px solid #ddd;">${new Date(item.applicationDate).toLocaleDateString()}</td>
                    <td style="padding:8px;border:1px solid #ddd;">${item.applicationType || '-'}</td>
                    <td style="padding:8px;border:1px solid #ddd;">${item.description || '-'}</td>
                </tr>`).join('');
        }
        document.getElementById("historyModal").style.display = "block";
    } catch (e) {
        showCustomAlert("Lỗi tải lịch sử kiểm kê: " + e.message, "Lỗi Hệ Thống", true);
    }
};

/* =========================================================================
   13. BULK ACTIONS
========================================================================= */
window.handleBulkAction = async function (action) {
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));

    if (!_swPerm.canRequest) {
        showCustomAlert(
            "You do not have permission to create requests.\nContact your Manager.",
            "Permission Denied", true
        );
        return;
    }

    const selectedIds = getSelectedSoftwareIds();

    if (selectedIds.length === 0) {
        showCustomAlert("Vui lòng chọn ít nhất 1 phần mềm!", "Thông báo");
        return;
    }

    if (action === 'group' || action === 'ungroup') {
        const msg       = action === 'group' ? "Group các phần mềm đã chọn?" : "Ungroup các phần mềm đã chọn?";
        const confirmed = await showCustomConfirm(msg, "Xác nhận");
        if (!confirmed) return;
        try {
            await fetchAPI(`/software/${action}`, {
                method: "POST",
                body:   JSON.stringify({ SoftwareIds: selectedIds })
            });
            showCustomAlert(`${action === 'group' ? 'Group' : 'Ungroup'} thành công!`, "Thành công");
            loadSoftwareManagementData();
        } catch (e) {
            showCustomAlert(`Lỗi ${action}: ` + e.message, "Lỗi Hệ Thống", true);
        }
        return;
    }

    if (action === 'change') {
        document.getElementById('bulkChangeCount').innerText = selectedIds.length;
        document.getElementById('bulkChangeModal').style.display = 'block';
    }
};

window.submitBulkChange = async function () {
    const approverId = document.getElementById("bulkChangeApp")?.value;
    const fieldName  = document.getElementById("bulkChangeField")?.value.trim();
    const newValue   = document.getElementById("bulkChangeValue")?.value.trim();

    if (!approverId) { showCustomAlert("Vui lòng chọn Approver!", "Cảnh báo", true); return; }
    if (!fieldName)  { showCustomAlert("Vui lòng nhập Field Name!", "Cảnh báo", true); return; }
    if (!newValue)   { showCustomAlert("Vui lòng nhập New Value!", "Cảnh báo", true); return; }

    const ok = await showCustomConfirm("Gửi Bulk Change Request?", "Xác nhận");
    if (!ok) return;

    const payload = {
        AssetIds:    getSelectedSoftwareIds(),
        FieldName:   fieldName,
        NewValue:    newValue,
        ApproverId:  parseInt(approverId),
        Description: document.getElementById("bulkChangeDesc")?.value || ""
    };

    try {
        await fetchAPI("/request/bulk/change", { method: "POST", body: JSON.stringify(payload) });
        showCustomAlert("Bulk Change Request đã được tạo!", "Thành công");
        closeModal("bulkChangeModal");
    } catch (e) {
        showCustomAlert("Lỗi: " + e.message, "Lỗi Hệ Thống", true);
    }
};

/* =========================================================================
   14. INSTALL SOFTWARE
   API: POST /software/install
   DTO: InstallSoftwareDto
========================================================================= */
window.openInstallSoftwareModal = function () {
    if (!_swPerm.canRequest) {
        showCustomAlert("You do not have permission to install software.", "Permission Denied", true);
        return;
    }
    const modal = document.getElementById('installSoftwareModal');
    modal.querySelectorAll('input:not([type="hidden"]), textarea').forEach(el => el.value = '');
    document.getElementById('instSwVendor').value      = '';
    document.getElementById('instSwLicense').value     = '';
    document.getElementById('instSwLicenseType').value = 'PAID';
    const box = document.getElementById('hwSuggestions');
    if (box) box.style.display = 'none';
    modal.style.display = 'block';
};

window.submitInstallSoftware = async function () {
    const payload = {
        AssetControlNumber: document.getElementById("instAssetControlNo").value.trim(),
        SoftwareName:       document.getElementById("instSwName").value.trim(),
        SoftwareVersion:    document.getElementById("instSwVersion").value.trim(),
        VendorId:           document.getElementById("instSwVendor").value  ? parseInt(document.getElementById("instSwVendor").value)  : null,
        LicenseId:          document.getElementById("instSwLicense").value ? parseInt(document.getElementById("instSwLicense").value) : null,
        LicenseType:        document.getElementById("instSwLicenseType").value.trim(),
        Description:        document.getElementById("instSwDesc").value.trim(),
        InstalledBy:        parseInt(localStorage.getItem("userId")) || 1
    };

    if (!payload.AssetControlNumber || !payload.SoftwareName || !payload.SoftwareVersion) {
        showCustomAlert("Vui lòng điền đầy đủ: Asset Control Number, Software Name, Version!", "Cảnh báo", true);
        return;
    }

    const ok = await showCustomConfirm(
        `Xác nhận cài đặt [${payload.SoftwareName}] lên thiết bị [${payload.AssetControlNumber}]?`,
        "Xác nhận Cài đặt"
    );
    if (!ok) return;

    try {
        await fetchAPI("/software/install", { method: "POST", body: JSON.stringify(payload) });
        showCustomAlert("Cài đặt phần mềm thành công!", "Thành công");
        closeModal('installSoftwareModal');
        loadSoftwareManagementData();
    } catch (e) {
        showCustomAlert("Lỗi cài đặt: " + e.message, "Lỗi Hệ Thống", true);
    }
};

/* =========================================================================
   15. AUTOCOMPLETE – Install modal & Copy modal
========================================================================= */
function setupInstallAutocomplete() {
    const input = document.getElementById('instAssetControlNo');
    const box   = document.getElementById('hwSuggestions');
    if (!input || !box) return;
    setupHwAutocomplete(input, box, val => {
        input.value       = val;
        box.style.display = 'none';
    });
}

function setupCopyAutocomplete() {
    const input = document.getElementById('copyTargetAsset');
    const box   = document.getElementById('copyHwSuggestions');
    if (!input || !box) return;
    setupHwAutocomplete(input, box, val => {
        input.value       = val;
        box.style.display = 'none';
    });
}

function setupHwAutocomplete(inputEl, suggestionBox, onSelect) {
    let timer = null;

    inputEl.addEventListener('input', function () {
        clearTimeout(timer);
        const keyword = this.value.trim();
        if (keyword.length < 2) { suggestionBox.style.display = 'none'; return; }

        timer = setTimeout(async () => {
            try {
                const res  = await fetchAPI("/hardware/search", {
                    method: "POST",
                    body:   JSON.stringify({ AssetControlNumber: keyword, Page: 1, PageSize: 10 })
                });
                const data = res.data || [];
                if (data.length > 0) {
                    suggestionBox.innerHTML = data.map(hw => `
                        <li style="padding:9px 12px;border-bottom:1px solid #eee;cursor:pointer;font-size:13px;"
                            onmouseover="this.style.background='#f0f4f8'"
                            onmouseout="this.style.background='white'"
                            onclick="(function(){ ${JSON.stringify(onSelect.toString())}; })()"
                        >
                            <strong>${hw.assetControlNumber}</strong>
                            <span style="color:#555;margin-left:8px;">${hw.assetName}</span>
                        </li>`).join('');

                    // Gắn onclick trực tiếp
                    suggestionBox.querySelectorAll('li').forEach((li, i) => {
                        li.onclick = () => onSelect(data[i].assetControlNumber);
                    });
                    suggestionBox.style.display = 'block';
                } else {
                    suggestionBox.innerHTML = `<li style="padding:9px 12px;color:#d32f2f;font-size:13px;">Không tìm thấy thiết bị</li>`;
                    suggestionBox.style.display = 'block';
                }
            } catch (e) { console.error("Autocomplete error:", e); }
        }, 400);
    });

    document.addEventListener('click', e => {
        if (e.target !== inputEl) suggestionBox.style.display = 'none';
    });
}

/* =========================================================================
   16. LOAD USERS (populate tất cả các select approver)
========================================================================= */
window.loadUserOptions = async function () {
    try {
        const res   = await fetchAPI("/users");
        const users = res.data || res;
        let html    = '<option value="">-- Select Approver --</option>';
        users.forEach(u => {
            html += `<option value="${u.userId}">${u.username}${u.email ? ' (' + u.email + ')' : ''}</option>`;
        });

        [
            'firstApp_swchange', 'secApp_swchange',
            'firstApp_copy',     'secApp_copy',
            'bulkChangeApp'
        ].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.innerHTML = html;
        });
    } catch (e) {
        console.error("Lỗi tải user:", e);
    }
};

window.toggleSecondaryApp = function (prefix) {
    const flag  = document.getElementById(`secAppFlag_${prefix}`);
    const sel   = document.getElementById(`secApp_${prefix}`);
    const badge = document.getElementById(`secAppBadge_${prefix}`);
    if (!flag || !sel) return;
    if (flag.checked) {
        sel.disabled          = false;
        if (badge) badge.style.display = 'inline-block';
    } else {
        sel.disabled          = true;
        sel.value             = '';
        if (badge) badge.style.display = 'none';
    }
};

/* =========================================================================
   17. CHECKBOX & DOWNLOAD
========================================================================= */
window.selectAllSoftwares = function (isSelect) {
    document.querySelectorAll('.sw-checkbox').forEach(cb => cb.checked = isSelect);
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
};

window.getSelectedSoftwareIds = function () {
    return Array.from(document.querySelectorAll('.sw-checkbox:checked')).map(cb => parseInt(cb.value));
};

window.downloadSelectedSoftwares = function () {
    const ids = getSelectedSoftwareIds();
    if (ids.length === 0) { showCustomAlert("Vui lòng chọn phần mềm!", "Thông báo"); return; }
    exportToCSV(softwareData.filter(s => ids.includes(s.softwareId)), 'Selected_Software.csv');
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
};

window.downloadAllSoftwares = function () {
    if (softwareData.length === 0) { showCustomAlert("Không có dữ liệu!", "Thông báo"); return; }
    exportToCSV(softwareData, 'All_Software.csv');
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
};

window.exportToCSV = function (dataArray, filename) {
    if (!dataArray || !dataArray.length) return;
    const headers  = Object.keys(dataArray[0]);
    const csvRows  = [headers.join(',')];
    for (const row of dataArray) {
        csvRows.push(headers.map(h => `"${String(row[h] ?? '').replace(/"/g, '""')}"`).join(','));
    }
    const blob = new Blob(['\uFEFF' + csvRows.join('\n')], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href  = URL.createObjectURL(blob);
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

/* =========================================================================
   18. CUSTOM MODAL UTILITIES
========================================================================= */
window.closeModal = function (modalId) {
    const el = document.getElementById(modalId);
    if (el) el.style.display = 'none';
};

window.showCustomAlert = function (message, title = "Notification", isError = false) {
    const msgEl = document.getElementById("alertMessage");
    if (!msgEl) { alert(`${title}:\n${message}`); return; }
    msgEl.innerText = message;
    const titleEl = document.getElementById("alertTitle");
    if (titleEl) titleEl.innerText = title;
    const header  = document.querySelector("#customAlertModal .modal-header");
    if (header)   header.style.backgroundColor = isError ? "#d32f2f" : "#000066";
    const modal   = document.getElementById("customAlertModal");
    if (modal)    modal.style.display = "block";
};

window.showCustomConfirm = function (message, title = "Confirm Action") {
    return new Promise(resolve => {
        const msgEl = document.getElementById("confirmMessage");
        if (!msgEl) { resolve(confirm(`${title}\n\n${message}`)); return; }
        msgEl.innerText = message;
        const titleEl = document.getElementById("confirmTitle");
        if (titleEl) titleEl.innerText = title;
        document.getElementById("customConfirmModal").style.display = "block";

        const btnYes = document.getElementById("btnConfirmYes");
        if (btnYes) {
            const newBtn = btnYes.cloneNode(true);
            btnYes.parentNode.replaceChild(newBtn, btnYes);
            newBtn.addEventListener("click", () => { closeModal("customConfirmModal"); resolve(true); });
        }
        const cancel = () => { closeModal("customConfirmModal"); resolve(false); };
        const btnCancel = document.querySelector("#customConfirmModal .btn-cancel");
        if (btnCancel) btnCancel.onclick = cancel;
        const btnClose  = document.querySelector("#customConfirmModal .close");
        if (btnClose)   btnClose.onclick  = cancel;
    });
};