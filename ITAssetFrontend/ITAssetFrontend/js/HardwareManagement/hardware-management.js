/* =========================================================================
   1. KHỞI TẠO VÀ BIẾN TOÀN CỤC
========================================================================= */
var hardwareData = [];
var hwTotal = 0;
var hwCurrentPage = 1;
var hwPageSize = 5;

// Local permission cache (populated by loadCurrentUserPermission / fallback)
var _hwPerm = window.currentPermission || {
    canView: true, canRequest: false, canApprove: false, canAdmin: false
};

window.initHardwareManagement = function () {
    // Re-read permission (may have been loaded by permission-guard.js already)
    _hwPerm = window.currentPermission || _hwPerm;
    _applyHwPermission();
    loadHardwareData();
    setupDropdownBehavior();
    loadDepartmentOptions();
    loadUserOptions();
};

// Called by permission-guard.js after it finishes loading
window.onPermissionLoaded = function(perm) {
    _hwPerm = perm;
    _applyHwPermission();
};

function _applyHwPermission() {
    // "+ Add New Hardware" buttons
    document.querySelectorAll(".hw-btn-add").forEach(function(el) {
        el.style.display = _hwPerm.canRequest ? "" : "none";
    });
    // Bulk action menu items that require canRequest
    document.querySelectorAll(".hw-bulk-action").forEach(function(el) {
        el.style.display = _hwPerm.canRequest ? "" : "none";
    });
    // Permission banner
    var banner = document.getElementById("permissionBanner");
    if (banner && typeof window.currentPermission !== "undefined") {
        // Let permission-guard render it
    }
}

async function loadHardwareData() {
    const listContainer = document.getElementById("hardwareList");
    if (!listContainer) return;
    
    listContainer.innerHTML = "<div style='padding: 20px;'>Loading hardware data...</div>";

    const controlNumber = document.getElementById("filterHwControlNumber")?.value.trim() || "";
    const assetName = document.getElementById("filterHwName")?.value.trim() || "";
    const manufacturer = document.getElementById("filterHwManufacturer")?.value.trim() || "";
    const model = document.getElementById("filterHwModel")?.value.trim() || "";

    const payload = {
        AssetControlNumber: controlNumber,
        AssetName: assetName,
        Manufacturer: manufacturer,
        Model: model,
        Page: hwCurrentPage,
        PageSize: hwPageSize
    };

    try {
        const response = await fetchAPI("/hardware/search", {
            method: "POST",
            body: JSON.stringify(payload)
        });

        hardwareData = response.data || [];
        hwTotal = response.total || 0;
        
        renderHardware();

    } catch (error) {
        console.error("Lỗi:", error);
        listContainer.innerHTML = `<div style="color: red; padding: 20px;">Failed to load data.</div>`;
    }
}

window.searchHardware = function() {
    hwCurrentPage = 1; 
    loadHardwareData();
}

window.clearHardwareSearch = function() {
    if(document.getElementById("filterHwControlNumber")) document.getElementById("filterHwControlNumber").value = "";
    if(document.getElementById("filterHwName")) document.getElementById("filterHwName").value = "";
    if(document.getElementById("filterHwManufacturer")) document.getElementById("filterHwManufacturer").value = "";
    if(document.getElementById("filterHwModel")) document.getElementById("filterHwModel").value = "";
    
    document.querySelectorAll('.tree-view input[type="checkbox"]').forEach(cb => cb.checked = false);
}

function renderHardware() {
    renderHardwareList();
    renderHardwarePagination();
}

function renderHardwareList() {
    const list = document.getElementById("hardwareList");

    if (hardwareData.length === 0) {
        list.innerHTML = "<div style='padding: 20px; text-align:center;'>No hardware found.</div>";
        return;
    }

    const pageData = hardwareData;

    // Build menu HTML per-card based on current user permission
    const menuFn = (typeof window.buildHardwareMenuHTML === "function")
        ? window.buildHardwareMenuHTML
        : function(id) {
            return '<a onclick="viewHwDetail(' + id + ')">&#9658; View details</a>';
          };

    list.innerHTML = pageData.map(h => `
        <div class="hardware-card">
            <div class="card-header">
                <div class="card-header-left">
                    <input type="checkbox" class="asset-checkbox" value="${h.assetId}">
                    <a onclick="viewHwDetail(${h.assetId})">View details</a>
                </div>
                <div class="card-header-right">
                    <div class="dropdown">
                        <button class="btn-menu" onclick="toggleHwDropdown(event, ${h.assetId})">Menu</button>
                        <div id="hw-dropdown-${h.assetId}" class="dropdown-content">
                            ${menuFn(h.assetId)}
                        </div>
                    </div>
                </div>
            </div>

            <div class="card-body">
                <div class="cell cell-label">Hardware Control Number</div>
                <div class="cell cell-value">${h.assetControlNumber || "-"}</div>
                <div class="cell cell-label">Machine Type</div>
                <div class="cell cell-value">${h.category || "-"}</div>

                <div class="cell cell-label">Asset Name</div>
                <div class="cell cell-value">${h.assetName || "-"}</div>
                <div class="cell cell-label">Location</div>
                <div class="cell cell-value">${h.location || "-"}</div>

                <div class="cell cell-label">Manufacturer</div>
                <div class="cell cell-value">${h.manufacturer || "-"}</div>
                <div class="cell cell-label">Model</div>
                <div class="cell cell-value">${h.model || "-"}</div>

                <div class="cell cell-label">Serial Number</div>
                <div class="cell cell-value">${h.serialNumber || "-"}</div>
                <div class="cell cell-label">Status</div>
                <div class="cell cell-value">${h.status || "-"}</div>
            </div>
        </div>
    `).join("");
}

function renderHardwarePagination() {
    const totalPages = Math.ceil(hwTotal / hwPageSize) || 1;
    let buttons = `
        <button onclick="changeHwPage(1)"><<</button>
        <button onclick="changeHwPage(${hwCurrentPage > 1 ? hwCurrentPage - 1 : 1})"><</button>
    `;

    for (let i = 1; i <= totalPages; i++) {
        buttons += `<button class="${i === hwCurrentPage ? "active" : ""}" onclick="changeHwPage(${i})">${i}</button>`;
    }

    buttons += `
        <button onclick="changeHwPage(${hwCurrentPage < totalPages ? hwCurrentPage + 1 : totalPages})">></button>
        <button onclick="changeHwPage(${totalPages})">>></button>
    `;

    const startItem = hwTotal === 0 ? 0 : ((hwCurrentPage - 1) * hwPageSize) + 1;
    const endItem = Math.min(hwCurrentPage * hwPageSize, hwTotal);

    if(document.getElementById("hwPaginationTop")) document.getElementById("hwPaginationTop").innerHTML = buttons;
    if(document.getElementById("hwPaginationBottom")) document.getElementById("hwPaginationBottom").innerHTML = buttons;
    
    if(document.getElementById("hwTotalTop")) document.getElementById("hwTotalTop").innerText = hwTotal;
    if(document.getElementById("hwTotalBottom")) document.getElementById("hwTotalBottom").innerText = hwTotal;
    
    if(document.getElementById("hwShowStartTop")) document.getElementById("hwShowStartTop").innerText = startItem;
    if(document.getElementById("hwShowEndTop")) document.getElementById("hwShowEndTop").innerText = endItem;
    if(document.getElementById("hwShowStartBottom")) document.getElementById("hwShowStartBottom").innerText = startItem;
    if(document.getElementById("hwShowEndBottom")) document.getElementById("hwShowEndBottom").innerText = endItem;
}

window.changeHwPage = function(page) {
    hwCurrentPage = page;
    loadHardwareData();
}

window.changeHwPageSize = function(size) {
    hwPageSize = parseInt(size);
    hwCurrentPage = 1;
    if(document.getElementById("hwPageSizeBottom")) document.getElementById("hwPageSizeBottom").value = size;
    if(document.getElementById("hwPageSizeTop")) document.getElementById("hwPageSizeTop").value = size;
    loadHardwareData();
}

/* === XỬ LÝ SỰ KIỆN DROPDOWN === */
window.toggleHwDropdown = function(event, id) {
    event.stopPropagation();
    const currentDropdown = document.getElementById(`hw-dropdown-${id}`);
    
    document.querySelectorAll('.dropdown-content').forEach(el => {
        if(el !== currentDropdown) el.classList.remove('show');
    });

    currentDropdown.classList.toggle("show");
}

function setupDropdownBehavior() {
    window.addEventListener("click", function(event) {
        if (!event.target.matches('.btn-menu') && !event.target.matches('.btn-outline')) {
            document.querySelectorAll('.dropdown-content').forEach(el => {
                if (el.classList.contains('show')) el.classList.remove('show');
            });
        }
    });
}

// Gọi API GET Hardware Detail
window.viewHwDetail = async function(id) { 
    try {
        const asset = await fetchAPI(`/hardware/${id}`);
        const info = `Mã thiết bị: ${asset.assetControlNumber}\nTên thiết bị: ${asset.assetName}\nNhà sản xuất: ${asset.manufacturer}\nModel: ${asset.model}\nSerial: ${asset.serialNumber}\nDanh mục: ${asset.category}\nVị trí: ${asset.location}\nTrạng thái: ${asset.status}`;
        showCustomAlert(info, "Thông tin Thiết bị");
    } catch (e) {
        showCustomAlert("Lỗi khi tải thông tin thiết bị: " + e.message, "Lỗi Hệ Thống", true);
    }
}

/* =========================================================================
   CÁC HÀM XỬ LÝ ACTION TỪ MENU (MỞ MODAL VÀ GỌI API)
========================================================================= */

window.handleMenuAction = function(action, assetId) {
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));

    // Actions that require canRequest permission
    const requestActions = ['move', 'change', 'disposal', 'failure', 'delete'];
    if (requestActions.includes(action) && !_hwPerm.canRequest) {
        showCustomAlert(
            "You do not have permission to create requests.\nContact your Manager.",
            "Permission Denied", true
        );
        return;
    }

    switch(action) {
        case 'move':
    // 1. Định nghĩa một hàm async dùng 1 lần (IIFE) để gọi API lấy chi tiết
    (async () => {
        try {
            // Lấy thông tin chi tiết bao gồm cả tên phòng ban
            const assetDetail = await fetchAPI(`/hardware/${assetId}`);
            
            // Gán dữ liệu vào Form
            document.getElementById('moveAssetId').value = assetId;
            document.getElementById('curMoveAssetName').value = assetDetail.assetName || "N/A";
            document.getElementById('curMoveControlNo').value = assetDetail.assetControlNumber || "N/A";
            document.getElementById('curMoveDept').value = assetDetail.departmentName || "None (Chưa phân bổ)";
            
            // Đặt lại (Reset) ô chọn đích đến và lý do
            document.getElementById('moveToDept').value = "";
            document.getElementById('reason_move').value = "";
            
            // Mở Modal
            document.getElementById('moveRequestModal').style.display = "block";
        } catch (e) {
            showCustomAlert("Lỗi khi lấy thông tin thiết bị: " + e.message, "Lỗi Hệ Thống", true);
        }
    })();
    break;
            
        case 'change': 
            // TÌM TÀI SẢN TRONG MẢNG VÀ ĐỔ DỮ LIỆU CŨ VÀO FORM
            const assetToChange = hardwareData.find(a => a.assetId === assetId);
            if (assetToChange) {
                document.getElementById('changeAssetId').value = assetId;
                
                const nameInput = document.getElementById('chg_asset_name');
                const mfgInput = document.getElementById('chg_manufacturer');
                const modelInput = document.getElementById('chg_model');
                
                // Gán value hiển thị và lưu giá trị gốc vào data-old để so sánh sau này
                if(nameInput) { nameInput.value = assetToChange.assetName || ""; nameInput.dataset.old = assetToChange.assetName || ""; }
                if(mfgInput) { mfgInput.value = assetToChange.manufacturer || ""; mfgInput.dataset.old = assetToChange.manufacturer || ""; }
                if(modelInput) { modelInput.value = assetToChange.model || ""; modelInput.dataset.old = assetToChange.model || ""; }
                
                document.getElementById('reason_change').value = "";
                document.getElementById('changeRequestModal').style.display = "block";
            } else {
                showCustomAlert("Không tìm thấy dữ liệu thiết bị!", "Lỗi Dữ Liệu", true);
            }
            break;

        case 'disposal': 
            openModal('disposalRequestModal', 'dispAssetId', assetId); 
            break;
        case 'failure': 
            openModal('failureRequestModal', 'failAssetId', assetId); 
            break;
        case 'appHistory': 
            loadApplicationHistory(assetId); 
            break;
        case 'invHistory': 
            loadInventoryHistory(assetId); 
            break;
        case 'delete':
            openDeleteHardwareModal(assetId);
            break;
    }
}

window.openModal = function(modalId, inputId, assetId) {
    const modal = document.getElementById(modalId);
    if(modal) {
        const inputs = modal.querySelectorAll('input:not([type="hidden"]):not([type="checkbox"]), textarea');
        inputs.forEach(input => input.value = '');
    }

    if(inputId && document.getElementById(inputId)) {
        document.getElementById(inputId).value = assetId;
    }
    
    if(modal) modal.style.display = "block";
}

window.closeModal = function(modalId) {
    const modal = document.getElementById(modalId);
    if(modal) modal.style.display = "none";
}

/* =====================================
   API: GET APPLICATION HISTORY
===================================== */
window.loadApplicationHistory = async function(assetId) {
    try {
        const data = await fetchAPI(`/request/history/${assetId}`);
        document.getElementById("historyModalTitle").innerText = `Application History - Asset ${assetId}`;
        
        let tableHTML = `
            <tr>
                <th>App ID</th><th>Applicant</th><th>Date</th><th>Type</th><th>Department</th><th>User</th>
            </tr>
        `;
        
        if (!data || data.length === 0) {
            tableHTML += `<tr><td colspan="6" style="text-align:center">No history found</td></tr>`;
        } else {
            data.forEach(item => {
                tableHTML += `
                    <tr>
                        <td>${item.applicationId}</td>
                        <td>${item.applicant || "-"}</td>
                        <td>${new Date(item.applicationDate).toLocaleDateString()}</td>
                        <td>${item.applicationType || "-"}</td>
                        <td>${item.department || "-"}</td>
                        <td>${item.user || "-"}</td>
                    </tr>
                `;
            });
        }

        document.getElementById("historyTable").innerHTML = tableHTML;
        document.getElementById("historyModal").style.display = "block";
    } catch (e) { showCustomAlert("Error loading history: " + e.message, "Lỗi Hệ Thống", true); }
}

/* =====================================
   API: GET INVENTORY HISTORY
===================================== */
window.loadInventoryHistory = async function(assetId) {
    try {
        const data = await fetchAPI(`/request/inventory-history/${assetId}`);
        document.getElementById("historyModalTitle").innerText = `Inventory History - Asset ${assetId}`;
        
        let tableHTML = `
            <tr>
                <th>Control No.</th><th>Manufacturer</th><th>Inv Date</th><th>Department</th><th>Implementer</th><th>Result</th>
            </tr>
        `;
        
        if (!data || data.length === 0) {
            tableHTML += `<tr><td colspan="6" style="text-align:center">No inventory history found</td></tr>`;
        } else {
            data.forEach(item => {
                tableHTML += `
                    <tr>
                        <td>${item.controlNumber || "-"}</td>
                        <td>${item.manufacturer || "-"}</td>
                        <td>${new Date(item.inventoryDate).toLocaleDateString()}</td>
                        <td>${item.department || "-"}</td>
                        <td>${item.implementer || "-"}</td>
                        <td>${item.result || "-"}</td>
                    </tr>
                `;
            });
        }

        document.getElementById("historyTable").innerHTML = tableHTML;
        document.getElementById("historyModal").style.display = "block";
    } catch (e) { showCustomAlert("Error loading inventory history: " + e.message, "Lỗi Hệ Thống", true); }
}

/* =========================================================================
   HÀM XỬ LÝ GIAO DIỆN PHÊ DUYỆT (APPROVAL)
========================================================================= */
window.toggleSecondaryApp = function(prefix) {
    const flagEl = document.getElementById(`secAppFlag_${prefix}`);
    const secAppSelect = document.getElementById(`secApp_${prefix}`);
    const badge = document.getElementById(`secAppBadge_${prefix}`);

    if(!flagEl || !secAppSelect || !badge) return;

    if(flagEl.checked) {
        secAppSelect.disabled = false;
        badge.style.display = "inline-block";
    } else {
        secAppSelect.disabled = true;
        secAppSelect.value = "";
        badge.style.display = "none";
    }
}

window.loadDepartmentOptions = async function() {
    const selectEl = document.getElementById("moveToDept");
    const bulkSelectEl = document.getElementById("bulkMoveDept"); 
    const addHwDeptEl = document.getElementById("addHwDept"); 
    
    try {
        const res = await fetchAPI("/departments?Page=1&PageSize=500");
        const departments = res.data; 
        
        let html = '<option value="">-- Select Target Department --</option>';
        departments.forEach(d => {
            html += `<option value="${d.departmentId}">${d.departmentName}</option>`;
        });
        
        if (selectEl) selectEl.innerHTML = html;
        if (bulkSelectEl) bulkSelectEl.innerHTML = html; 
        if (addHwDeptEl) addHwDeptEl.innerHTML = html; 
        
    } catch (error) {
        console.error("Lỗi tải danh sách phòng ban:", error);
    }
}

window.loadUserOptions = async function() {
    try {
        const res = await fetchAPI("/users");
        const users = res.data || res;
        
        let html = '<option value="">-- Select Approver --</option>';
        users.forEach(u => {
            html += `<option value="${u.userId}">${u.username} (${u.email})</option>`;
        });
        
        const bulkApproverIds = ['bulkMoveApp', 'bulkChangeApp', 'bulkDispApp', 'bulkFailApp'];
        bulkApproverIds.forEach(id => {
            const selectEl = document.getElementById(id);
            if (selectEl) selectEl.innerHTML = html;
        });

        const singleApproverIds = [
            'firstApp_move', 'secApp_move',
            'firstApp_change', 'secApp_change',
            'firstApp_disp', 'secApp_disp',
            'firstApp_fail', 'secApp_fail'
        ];
        singleApproverIds.forEach(id => {
            const selectEl = document.getElementById(id);
            if (selectEl) selectEl.innerHTML = html;
        });
        
    } catch (error) {
        console.error("Lỗi tải danh sách người dùng:", error);
    }
}

/* =====================================
   API: SUBMIT THAO TÁC ĐƠN
===================================== */
window.submitMoveRequest = async function() {
    const firstApp = document.getElementById("firstApp_move").value;
    const secApp = document.getElementById("secApp_move").value;
    const isSecAppOn = document.getElementById("secAppFlag_move").checked;

    if(!firstApp || (isSecAppOn && !secApp)) {
        showCustomAlert("Vui lòng chọn đầy đủ người phê duyệt (Approvers)!", "Thông báo");
        return;
    }

    const isConfirmed = await showCustomConfirm("Bạn có chắc chắn muốn gửi Yêu cầu Di dời (Move Request)?", "Xác nhận");
    if(!isConfirmed) return;

    const payload = {
        AssetId: parseInt(document.getElementById("moveAssetId").value),
        UserCreatedId: 1, 
        Description: document.getElementById("reason_move").value,
        MoveToDepartmentId: parseInt(document.getElementById("moveToDept").value || 0),
        FirstApproverId: parseInt(firstApp),
        SecondApproverId: isSecAppOn ? parseInt(secApp) : null
    };

    try {
        await fetchAPI("/request/move", { method: "POST", body: JSON.stringify(payload) });
        showCustomAlert("Move request created successfully!", "Thành công");
        closeModal("moveRequestModal");
    } catch (e) { showCustomAlert("Error: " + e.message, "Lỗi Hệ Thống", true); }
}

window.submitChangeRequest = async function() {
    const firstApp = document.getElementById("firstApp_change").value;
    if(!firstApp) { showCustomAlert("Vui lòng chọn First Approver!", "Thông báo", true); return; }

    // 1. Quét tất cả các ô input có class 'chg-input' để tìm sự thay đổi
    const changes = [];
    const inputs = document.querySelectorAll('.chg-input');
    
    inputs.forEach(input => {
        const oldVal = input.dataset.old || "";
        const newVal = input.value.trim();
        
        // Nếu dữ liệu mới khác dữ liệu cũ, thêm vào mảng Changes
        if (oldVal !== newVal) {
            changes.push({
                FieldName: input.dataset.field, // Lấy tên cột DB (ví dụ: asset_name)
                OldValue: oldVal,
                NewValue: newVal
            });
        }
    });

    // 2. Chặn Submit nếu người dùng chưa sửa chữ nào
    if (changes.length === 0) {
        showCustomAlert("Bạn chưa thay đổi bất kỳ thông tin nào!", "Thông báo", true);
        return;
    }

    const isConfirmed = await showCustomConfirm("Bạn có chắc chắn muốn gửi Yêu cầu Thay đổi (Change Request)?", "Xác nhận");
    if(!isConfirmed) return;

    // 3. Đóng gói Payload với mảng các trường đã thay đổi
    const payload = {
        AssetId: parseInt(document.getElementById("changeAssetId").value),
        UserCreatedId: 1, 
        Description: document.getElementById("reason_change").value,
        ApproverId: parseInt(firstApp), 
        Changes: changes // Truyền mảng động vào đây
    };

    try {
        await fetchAPI("/request/change", { method: "POST", body: JSON.stringify(payload) });
        showCustomAlert("Tạo yêu cầu thay đổi thành công!", "Thành công");
        closeModal("changeRequestModal");
    } catch (e) { showCustomAlert("Error: " + e.message, "Lỗi Hệ Thống", true); }
}

window.submitDisposalRequest = async function() {
    const firstApp = document.getElementById("firstApp_disp").value;
    if(!firstApp) { showCustomAlert("Vui lòng chọn First Approver!", "Thông báo"); return; }

    const isConfirmed = await showCustomConfirm("Bạn có chắc chắn muốn gửi Yêu cầu Thanh lý/Trả lại?", "Xác nhận");
    if(!isConfirmed) return;

    const payload = {
        AssetId: parseInt(document.getElementById("dispAssetId").value),
        UserCreatedId: 1,
        Description: document.getElementById("reason_disp").value,
        Type: document.getElementById("dispType").value,
        ApproverId: parseInt(firstApp)
    };

    try {
        await fetchAPI("/request/disposal", { method: "POST", body: JSON.stringify(payload) });
        showCustomAlert("Disposal/Return request created!", "Thành công");
        closeModal("disposalRequestModal");
    } catch (e) { showCustomAlert("Error: " + e.message, "Lỗi Hệ Thống", true); }
}

window.submitFailureRequest = async function() {
    const firstApp = document.getElementById("firstApp_fail").value;
    if(!firstApp) { showCustomAlert("Vui lòng chọn First Approver!", "Thông báo"); return; }

    const isConfirmed = await showCustomConfirm("Bạn có chắc chắn muốn gửi Yêu cầu Báo hỏng (Failure Request)?", "Xác nhận");
    if(!isConfirmed) return;

    const payload = {
        AssetId: parseInt(document.getElementById("failAssetId").value),
        UserCreatedId: 1,
        Description: document.getElementById("reason_fail").value,
        ApplicationClassification: document.getElementById("failClass").value,
        PickupDate: document.getElementById("failPickup").value,
        ReceiptDate: document.getElementById("failReceipt").value,
        BreakdownReason: document.getElementById("failBreakdownReason").value,
        ApproverId: parseInt(firstApp)
    };

    try {
        await fetchAPI("/request/failure", { method: "POST", body: JSON.stringify(payload) });
        showCustomAlert("Failure request created!", "Thành công");
        closeModal("failureRequestModal");
    } catch (e) { showCustomAlert("Error: " + e.message, "Lỗi Hệ Thống", true); }
}

/* =========================================================================
   XỬ LÝ BULK OPERATION MENU VÀ CHECKBOX (CHỌN/BỎ CHỌN TẤT CẢ)
========================================================================= */

window.toggleBulkDropdown = function(event, btnElement) {
    event.stopPropagation();
    const dropdown = btnElement.nextElementSibling;
    
    document.querySelectorAll('.dropdown-content').forEach(el => {
        if(el !== dropdown) el.classList.remove('show');
    });

    dropdown.classList.toggle("show");
}

window.selectAllAssets = function(isSelect) {
    const checkboxes = document.querySelectorAll('.asset-checkbox');
    checkboxes.forEach(cb => cb.checked = isSelect); 
    
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
}

window.getSelectedAssetIds = function() {
    const checkboxes = document.querySelectorAll('.asset-checkbox:checked');
    return Array.from(checkboxes).map(cb => parseInt(cb.value));
}

window.handleBulkAction = function(action) {
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));

    const selectedIds = getSelectedAssetIds();
    if (selectedIds.length === 0) {
        showCustomAlert("Vui lòng chọn ít nhất 1 thiết bị bằng cách tích vào checkbox!", "Thông báo");
        return;
    }

    switch(action) {
        case 'change':
            openModal('bulkChangeModal', null, null);
            document.getElementById('bulkChangeCount').innerText = selectedIds.length;
            break;
        case 'move':
            openModal('bulkMoveModal', null, null);
            document.getElementById('bulkMoveCount').innerText = selectedIds.length;
            break;
        case 'disposal':
            openModal('bulkDisposalModal', null, null);
            document.getElementById('bulkDispCount').innerText = selectedIds.length;
            break;
        case 'failure':
            openModal('bulkFailureModal', null, null);
            document.getElementById('bulkFailCount').innerText = selectedIds.length;
            break;
    }
}

/* =========================================================================
   XỬ LÝ TẢI DỮ LIỆU (DOWNLOAD CSV)
========================================================================= */

window.downloadSelectedAssets = function() {
    const selectedIds = getSelectedAssetIds();
    if (selectedIds.length === 0) {
        showCustomAlert("Vui lòng chọn ít nhất 1 thiết bị để tải xuống!", "Thông báo");
        return;
    }
    
    const selectedData = hardwareData.filter(h => selectedIds.includes(h.assetId));
    exportToCSV(selectedData, 'Selected_Hardware_Assets.csv');
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
}

window.downloadAllAssets = function() {
    if (hardwareData.length === 0) {
        showCustomAlert("Không có dữ liệu để tải xuống!", "Thông báo");
        return;
    }
    exportToCSV(hardwareData, 'All_Hardware_Assets.csv');
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
}

window.exportToCSV = function(dataArray, filename) {
    if (!dataArray || !dataArray.length) return;

    const headers = Object.keys(dataArray[0]);
    const csvRows = [];
    csvRows.push(headers.join(','));

    for (const row of dataArray) {
        const values = headers.map(header => {
            const val = row[header];
            const stringVal = val !== null && val !== undefined ? String(val) : '';
            const escapedVal = stringVal.replace(/"/g, '""');
            return `"${escapedVal}"`;
        });
        csvRows.push(values.join(','));
    }

    const csvString = '\uFEFF' + csvRows.join('\n');
    const blob = new Blob([csvString], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', filename);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

/* =========================================================================
   API: GỬI YÊU CẦU BULK (HÀNG LOẠT)
========================================================================= */

window.submitBulkChange = async function() {
    const approverId = document.getElementById("bulkChangeApp").value;
    if(!approverId) { showCustomAlert("Vui lòng chọn Approver ID!", "Thông báo"); return; }

    const isConfirmed = await showCustomConfirm("Gửi Yêu cầu Thay đổi Hàng loạt (Bulk Change)?", "Xác nhận");
    if(!isConfirmed) return;

    const payload = {
        AssetIds: getSelectedAssetIds(),
        Description: document.getElementById("bulkChangeDesc").value,
        FieldName: document.getElementById("bulkChangeField").value,
        NewValue: document.getElementById("bulkChangeValue").value,
        ApproverId: parseInt(approverId)
    };

    try {
        await fetchAPI("/request/bulk/change", { method: "POST", body: JSON.stringify(payload) });
        showCustomAlert("Bulk Change request created successfully!", "Thành công");
        closeModal("bulkChangeModal");
    } catch (e) { showCustomAlert("Error: " + e.message, "Lỗi Hệ Thống", true); }
}

window.submitBulkMove = async function() {
    const approverId = document.getElementById("bulkMoveApp").value;
    if(!approverId) { showCustomAlert("Vui lòng chọn Approver ID!", "Thông báo"); return; }

    const isConfirmed = await showCustomConfirm("Gửi Yêu cầu Di dời Hàng loạt (Bulk Move)?", "Xác nhận");
    if(!isConfirmed) return;

    const payload = {
        AssetIds: getSelectedAssetIds(),
        Description: document.getElementById("bulkMoveDesc").value,
        NewDepartmentId: parseInt(document.getElementById("bulkMoveDept").value || 0),
        NewLocationId: parseInt(document.getElementById("bulkMoveLoc") ? document.getElementById("bulkMoveLoc").value : 0),
        ApproverId: parseInt(approverId)
    };

    try {
        await fetchAPI("/request/bulk/move", { method: "POST", body: JSON.stringify(payload) });
        showCustomAlert("Bulk Move request created successfully!", "Thành công");
        closeModal("bulkMoveModal");
    } catch (e) { showCustomAlert("Error: " + e.message, "Lỗi Hệ Thống", true); }
}

window.submitBulkDisposal = async function() {
    const approverId = document.getElementById("bulkDispApp").value;
    if(!approverId) { showCustomAlert("Vui lòng chọn Approver ID!", "Thông báo"); return; }

    const isConfirmed = await showCustomConfirm("Gửi Yêu cầu Thanh lý/Trả lại Hàng loạt?", "Xác nhận");
    if(!isConfirmed) return;

    const payload = {
        AssetIds: getSelectedAssetIds(),
        Description: document.getElementById("bulkDispDesc").value,
        IsDisposal: document.getElementById("bulkDispType").value === "true",
        ApproverId: parseInt(approverId)
    };

    try {
        await fetchAPI("/request/bulk/disposal", { method: "POST", body: JSON.stringify(payload) });
        showCustomAlert("Bulk Disposal/Return request created successfully!", "Thành công");
        closeModal("bulkDisposalModal");
    } catch (e) { showCustomAlert("Error: " + e.message, "Lỗi Hệ Thống", true); }
}

window.submitBulkFailure = async function() {
    const approverId = document.getElementById("bulkFailApp").value;
    if(!approverId) { showCustomAlert("Vui lòng chọn Approver ID!", "Thông báo"); return; }

    const isConfirmed = await showCustomConfirm("Gửi Yêu cầu Báo hỏng Hàng loạt (Bulk Failure)?", "Xác nhận");
    if(!isConfirmed) return;

    const payload = {
        AssetIds: getSelectedAssetIds(),
        Description: document.getElementById("bulkFailDesc").value,
        ApplicationClassification: document.getElementById("bulkFailClass").value,
        PickupDate: document.getElementById("bulkFailPickup").value,
        ReceiptDate: document.getElementById("bulkFailReceipt").value,
        BreakdownReason: document.getElementById("bulkFailReason").value,
        ApproverId: parseInt(approverId)
    };

    try {
        await fetchAPI("/request/bulk/failure", { method: "POST", body: JSON.stringify(payload) });
        showCustomAlert("Bulk Failure request created successfully!", "Thành công");
        closeModal("bulkFailureModal");
    } catch (e) { showCustomAlert("Error: " + e.message, "Lỗi Hệ Thống", true); }
}

/* =========================================================================
   CÁC HÀM TIỆN ÍCH: CUSTOM CONFIRM VÀ CUSTOM ALERT
========================================================================= */

window.showCustomAlert = function(message, title = "Notification", isError = false) {
    const alertMsgEl = document.getElementById("alertMessage");
    
    if (!alertMsgEl) {
        alert(`${title}:\n${message}`);
        return;
    }
    
    alertMsgEl.innerText = message;
    
    const alertTitleEl = document.getElementById("alertTitle");
    if (alertTitleEl) alertTitleEl.innerText = title;
    
    const header = document.querySelector("#customAlertModal .modal-header");
    if(header) {
        header.style.backgroundColor = isError ? "#d32f2f" : "#000066"; 
    }
    
    const modal = document.getElementById("customAlertModal");
    if (modal) modal.style.display = "block";
}

window.showCustomConfirm = function(message, title = "Confirm Action") {
    return new Promise((resolve) => {
        const confirmMsgEl = document.getElementById("confirmMessage");
        
        if (!confirmMsgEl) {
            const result = confirm(`${title}\n\n${message}`);
            resolve(result);
            return;
        }

        confirmMsgEl.innerText = message;
        
        const confirmTitleEl = document.getElementById("confirmTitle");
        if (confirmTitleEl) confirmTitleEl.innerText = title;
        
        document.getElementById("customConfirmModal").style.display = "block";

        const btnYes = document.getElementById("btnConfirmYes");
        if (btnYes) {
            const newBtnYes = btnYes.cloneNode(true);
            btnYes.parentNode.replaceChild(newBtnYes, btnYes);
            newBtnYes.addEventListener("click", () => {
                closeModal("customConfirmModal");
                resolve(true); 
            });
        }

        const handleCancel = () => {
            closeModal("customConfirmModal");
            resolve(false); 
        };

        const btnCancel = document.querySelector("#customConfirmModal .btn-cancel");
        if (btnCancel) btnCancel.onclick = handleCancel;
        
        const btnClose = document.querySelector("#customConfirmModal .close");
        if (btnClose) btnClose.onclick = handleCancel;
    });
}

/* =========================================================================
   XÓA PHẦN CỨNG (DELETE HARDWARE)
========================================================================= */
window.openDeleteHardwareModal = function(assetId) {
    // Permission check
    if (!_hwPerm.canRequest) {
        showCustomAlert("You do not have permission to delete assets.", "Permission Denied", true);
        return;
    }

    const asset = hardwareData.find(a => a.assetId === assetId);
    if (!asset) {
        showCustomAlert("Không tìm thấy thông tin thiết bị!", "Lỗi Dữ Liệu", true);
        return;
    }

    document.getElementById('deleteHwAssetId').value        = assetId;
    document.getElementById('deleteHwControlNo').innerText  = asset.assetControlNumber || '-';
    document.getElementById('deleteHwName').innerText       = asset.assetName          || '-';
    document.getElementById('deleteHwSerial').innerText     = asset.serialNumber       || '-';
    document.getElementById('deleteHwStatus').innerText     = asset.status             || '-';

    const btnConfirm = document.getElementById('btnConfirmDeleteHw');
    if (btnConfirm) { btnConfirm.disabled = false; btnConfirm.innerText = 'Yes, Delete'; }

    document.getElementById('deleteHardwareModal').style.display = 'block';
}

window.executeDeleteHardware = async function() {
    const assetId  = parseInt(document.getElementById('deleteHwAssetId').value);
    const btnConfirm = document.getElementById('btnConfirmDeleteHw');

    if (!assetId) return;

    if (btnConfirm) { btnConfirm.disabled = true; btnConfirm.innerText = 'Deleting...'; }

    try {
        await fetchAPI(`/hardware/${assetId}`, { method: 'DELETE' });
        closeModal('deleteHardwareModal');
        showCustomAlert('Hardware deleted successfully.', 'Success');
        loadHardwareData();
    } catch (e) {
        showCustomAlert('Delete failed: ' + e.message, 'Lỗi Hệ Thống', true);
    } finally {
        if (btnConfirm) { btnConfirm.disabled = false; btnConfirm.innerText = 'Yes, Delete'; }
    }
}

/* =========================================================================
   THÊM MỚI PHẦN CỨNG (ADD HARDWARE)
========================================================================= */
window.openAddHardwareModal = function() {
    // Permission check
    if (!_hwPerm.canRequest) {
        showCustomAlert("You do not have permission to add assets.", "Permission Denied", true);
        return;
    }

    const modal = document.getElementById('addHardwareModal');
    modal.querySelectorAll('input').forEach(i => i.value = '');
    document.getElementById('addHwCategory').value = "1";
    document.getElementById('addHwStatus').value = "2";
    document.getElementById('addHwLocation').value = "1";
    document.getElementById('addHwDept').value = "";
    
    modal.style.display = 'block';
}

window.submitAddHardware = async function() {
    // 1. Thu thập dữ liệu từ Form
    const payload = {
        assetControlNumber: document.getElementById("addHwControlNo").value.trim(),
        assetName: document.getElementById("addHwName").value.trim(),
        manufacturer: document.getElementById("addHwMfg").value.trim(),
        model: document.getElementById("addHwModel").value.trim(),
        serialNumber: document.getElementById("addHwSerial").value.trim(),
        categoryId: parseInt(document.getElementById("addHwCategory").value),
        statusId: parseInt(document.getElementById("addHwStatus").value),
        locationId: parseInt(document.getElementById("addHwLocation").value),
        departmentId: parseInt(document.getElementById("addHwDept").value)
    };

    // 2. Validate dữ liệu bắt buộc (Must)
    if (!payload.assetControlNumber || !payload.assetName || !payload.serialNumber || isNaN(payload.departmentId)) {
        showCustomAlert("Vui lòng điền đầy đủ các trường bắt buộc (Must) có đánh dấu đỏ!", "Cảnh báo", true);
        return;
    }

    // 3. Gửi API
    const isConfirmed = await showCustomConfirm("Lưu thông tin Phần cứng mới vào hệ thống?", "Xác nhận");
    if (!isConfirmed) return;

    try {
        await fetchAPI("/hardware", {
            method: "POST",
            body: JSON.stringify(payload)
        });
        showCustomAlert("Thêm mới thiết bị phần cứng thành công!", "Thành công");
        closeModal("addHardwareModal");
        loadHardwareData();
    } catch (error) {
        showCustomAlert("Lỗi khi thêm mới (Có thể trùng Mã/Serial): " + error.message, "Lỗi Hệ Thống", true);
    }
}