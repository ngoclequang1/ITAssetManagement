var userData = [];
var userTotal = 0;
var userCurrentPage = 1;
var userPageSize = 4; 

window.initUserList = function () {
    loadUserData();
};

async function loadUserData() {
    const listContainer = document.getElementById("userListContainer");
    if (!listContainer) return;

    listContainer.innerHTML = "<div style='padding: 20px;'>Loading user data...</div>";

    const userCode = document.getElementById("filterUserCode").value.trim();
    const username = document.getElementById("filterUsername").value.trim();
    const consoleLoginId = document.getElementById("filterConsoleLoginId").value.trim();
    const email = document.getElementById("filterEmail").value.trim();
    const deptCode = document.getElementById("filterDeptCode").value.trim();
    
    const isAuditor = document.getElementById("filterAuditor").checked;
    const isNonAuditor = document.getElementById("filterNonAuditor").checked;

    const roleCheckboxes = document.querySelectorAll('.filter-group .checkbox-container input[type="checkbox"]:checked');
    const roleIds = Array.from(roleCheckboxes)
                         .map(cb => cb.value)
                         .filter(val => val && val !== "on"); 

    let params = new URLSearchParams();
    params.append("Page", userCurrentPage);
    params.append("PageSize", userPageSize);

    let isSearch = false; 

    if(userCode) { params.append("UserCode", userCode); isSearch = true; }
    if(username) { params.append("Username", username); isSearch = true; }
    if(consoleLoginId) { params.append("ConsoleLoginId", consoleLoginId); isSearch = true; }
    if(email) { params.append("Email", email); isSearch = true; }
    if(deptCode) { params.append("DepartmentCode", deptCode); isSearch = true; }
    
    if (isAuditor && !isNonAuditor) { params.append("AuditorFlag", "true"); isSearch = true; }
    if (!isAuditor && isNonAuditor) { params.append("AuditorFlag", "false"); isSearch = true; }

    if (roleIds.length > 0) {
        roleIds.forEach(id => params.append("RoleIds", id));
        isSearch = true;
    }
    
    let url = isSearch ? `/users/search?${params.toString()}` : `/users?${params.toString()}`;

    try {
        const response = await fetchAPI(url);
        userData = response.data || []; 
        userTotal = response.total || 0;
        
        renderUserList();
        renderUserPagination();

    } catch (error) {
        console.error("Lỗi tải User List:", error);
        listContainer.innerHTML = `<div style="color: red; padding: 20px;">Failed to load user data.</div>`;
    }
}

window.searchUsers = function() {
    userCurrentPage = 1; 
    loadUserData();
}

window.clearUserSearch = function() {
    document.getElementById("filterUserCode").value = "";
    document.getElementById("filterUsername").value = "";
    document.getElementById("filterConsoleLoginId").value = "";
    document.getElementById("filterEmail").value = "";
    document.getElementById("filterDeptCode").value = "";
    document.getElementById("filterAuditor").checked = false;
    document.getElementById("filterNonAuditor").checked = false;
    
    document.querySelectorAll('.checkbox-container input[type="checkbox"]').forEach(cb => cb.checked = false);
}

window.resetUserSearch = function() {
    clearUserSearch();
    searchUsers();
}

/* =========================================================================
   RENDER DỮ LIỆU & MENU
========================================================================= */

function renderUserList() {
    const list = document.getElementById("userListContainer");

    if (userData.length === 0) {
        list.innerHTML = "<div style='padding: 20px; text-align:center;'>No users found.</div>";
        return;
    }

    const pageData = userData;

    list.innerHTML = pageData.map(u => `
        <div class="user-card">
            <div class="card-header">
                <div class="card-header-left">
                    <input type="checkbox" class="user-checkbox" value="${u.userId}">
                    <a onclick="viewUserDetail(${u.userId})">View details</a>
                </div>
                <div class="card-header-right">
                    <div class="dropdown">
                        <button class="btn-menu" onclick="toggleUserDropdown(event, ${u.userId})">Menu</button>
                        <div id="u-dropdown-${u.userId}" class="dropdown-content" style="right: 0; left: auto; min-width: 200px;">
                            <a onclick="handleUserMenuAction('changes', ${u.userId})">▶ Changes</a>
                            <a onclick="handleUserMenuAction('delete', ${u.userId})">▶ Delete</a>
                            <a onclick="handleUserMenuAction('description', ${u.userId})">▶ Description</a>
                            <a onclick="handleUserMenuAction('copy', ${u.userId})">▶ Copy registration</a>
                            <a onclick="handleUserMenuAction('hardware', ${u.userId})">▶ List of hardware used</a>
                        </div>
                    </div>
                </div>
            </div>

            <div class="card-body">
                <div class="cell cell-label">User Code</div>
                <div class="cell cell-value">${u.userCode || "-"}</div>
                <div class="cell cell-label">Username</div>
                <div class="cell cell-value">${u.username || "-"}</div>

                <div class="cell cell-label">Console Login ID</div>
                <div class="cell cell-value">${u.consoleLoginId || "-"}</div>
                <div class="cell cell-label">System Login ID</div>
                <div class="cell cell-value">${u.systemLoginId || "-"}</div>

                <div class="cell cell-label">Email address</div>
                <div class="cell cell-value">${u.email || "-"}</div>
                <div class="cell cell-label">Primary department code</div>
                <div class="cell cell-value">${u.departmentCode || "0"}</div>

                <div class="cell cell-label">Name of the main department</div>
                <div class="cell cell-value">${u.departmentName || "-"}</div>
                <div class="cell cell-label">Permissions</div>
                <div class="cell cell-value">${u.roleName || "General Users"}</div>

                <div class="cell cell-label">Update with System Login ID</div>
                <div class="cell cell-value">Do not update</div>
                <div class="cell cell-label"></div>
                <div class="cell cell-value"></div>
            </div>
        </div>
    `).join("");
}

function renderUserPagination() {
    const totalPages = Math.ceil(userTotal / userPageSize) || 1;
    let buttons = `
        <button onclick="changeUserPage(1)"><<</button>
        <button onclick="changeUserPage(${userCurrentPage > 1 ? userCurrentPage - 1 : 1})"><</button>
    `;

    for (let i = 1; i <= totalPages; i++) {
        buttons += `<button class="${i === userCurrentPage ? "active" : ""}" onclick="changeUserPage(${i})">${i}</button>`;
    }

    buttons += `
        <button onclick="changeUserPage(${userCurrentPage < totalPages ? userCurrentPage + 1 : totalPages})">></button>
        <button onclick="changeUserPage(${totalPages})">>></button>
    `;

    const startItem = userTotal === 0 ? 0 : ((userCurrentPage - 1) * userPageSize) + 1;
    const endItem = Math.min(userCurrentPage * userPageSize, userTotal);

    document.getElementById("uPaginationTop").innerHTML = buttons;
    document.getElementById("uPaginationBottom").innerHTML = buttons;
    
    document.getElementById("uTotalTop").innerText = userTotal;
    document.getElementById("uTotalBottom").innerText = userTotal;
    document.getElementById("uTotalAffTop").innerText = userTotal;
    document.getElementById("uTotalAffMaxTop").innerText = userTotal;
    
    document.getElementById("uShowStartTop").innerText = startItem;
    document.getElementById("uShowEndTop").innerText = endItem;
    document.getElementById("uShowStartBottom").innerText = startItem;
    document.getElementById("uShowEndBottom").innerText = endItem;
}

window.changeUserPage = function(page) {
    userCurrentPage = page;
    loadUserData(); 
}

window.changeUserPageSize = function(size) {
    userPageSize = parseInt(size);
    userCurrentPage = 1;
    document.getElementById("uPageSizeBottom").value = size;
    document.getElementById("uPageSizeTop").value = size;
    loadUserData(); 
}

/* =========================================================================
   XỬ LÝ DROPDOWN & MENU ACTIONS
========================================================================= */

window.toggleUserDropdown = function(event, id) {
    event.stopPropagation();
    const currentDropdown = document.getElementById(`u-dropdown-${id}`);
    
    document.querySelectorAll('.dropdown-content').forEach(el => {
        if(el !== currentDropdown) el.classList.remove('show');
    });

    currentDropdown.classList.toggle("show");
}

// Ẩn menu khi click ra ngoài
document.addEventListener("click", function(event) {
    if (!event.target.matches('.btn-menu') && !event.target.matches('.btn-outline')) {
        document.querySelectorAll('.dropdown-content').forEach(el => {
            if (el.classList.contains('show')) el.classList.remove('show');
        });
    }
});

window.handleUserMenuAction = async function(action, userId) {
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));

    switch(action) {
        case 'changes':
            openEditUserModal(userId); 
            break;
            
        case 'delete':
            const isConfirmedDelete = await showCustomConfirm(
                `Bạn có chắc chắn muốn XÓA User ID ${userId} không? Hành động này không thể hoàn tác!`, 
                "Xác nhận Xóa"
            );
            if(isConfirmedDelete) {
                try {
                    await fetchAPI(`/users/${userId}`, { method: "DELETE" });
                    showCustomAlert("Xóa thành công!", "Thành công");
                    loadUserData(); // Reload lại danh sách sau khi xóa
                } catch(e) { 
                    showCustomAlert("Lỗi khi xóa: " + e.message, "Lỗi Hệ Thống", true); 
                }
            }
            break;
            
        case 'description':
            viewUserDetail(userId); 
            break;
            
        case 'copy':
            const isConfirmedCopy = await showCustomConfirm(
                `Bạn muốn tạo một bản sao của User ID ${userId}?`,
                "Xác nhận Copy"
            );
            if(isConfirmedCopy) {
                try {
                    await fetchAPI(`/users/${userId}/copy`, { method: "POST" });
                    showCustomAlert("Tạo bản sao thành công!", "Thành công");
                    loadUserData(); // Reload lại danh sách để thấy user mới
                } catch(e) { 
                    showCustomAlert("Lỗi khi tạo bản sao: " + e.message, "Lỗi Hệ Thống", true); 
                }
            }
            break;
            
        case 'hardware':
            try {
                const hwData = await fetchAPI(`/users/${userId}/hardware`);
                if(hwData.length === 0) {
                    showCustomAlert("Người dùng này hiện không được cấp phát tài sản phần cứng nào.", "Thông tin");
                } else {
                    const hwList = hwData.map(h => `- ${h.assetControlNumber}: ${h.assetName} (${h.model})`).join("\n");
                    showCustomAlert(`Danh sách thiết bị đang sử dụng:\n${hwList}`, "Phần cứng đã cấp phát");
                }
            } catch(e) { 
                showCustomAlert("Lỗi khi lấy danh sách phần cứng: " + e.message, "Lỗi Hệ Thống", true); 
            }
            break;
    }
}

/* =========================================================================
   CÁC HÀM XỬ LÝ GET DETAIL VÀ UPDATE
========================================================================= */

window.closeModal = function(modalId) {
    document.getElementById(modalId).style.display = "none";
}

// 1. Mở Modal Xem chi tiết (Description)
window.viewUserDetail = async function(id) {
    try {
        const user = await fetchAPI(`/users/${id}`);
        
        document.getElementById("detailUserId").innerText = user.userId || "-";
        document.getElementById("detailUserCode").innerText = user.userCode || "-";
        document.getElementById("detailUsername").innerText = user.username || "-";
        document.getElementById("detailEmail").innerText = user.email || "-";
        document.getElementById("detailConsoleId").innerText = user.consoleLoginId || "-";
        document.getElementById("detailSystemId").innerText = user.systemLoginId || "-";
        document.getElementById("detailDeptCode").innerText = user.departmentCode || "-";
        document.getElementById("detailDeptName").innerText = user.departmentName || "-";
        document.getElementById("detailRole").innerText = user.roleName || "-";
        document.getElementById("detailAuditor").innerText = user.auditorFlag ? "Yes (Auditor)" : "No (Non-auditor)";

        document.getElementById("userDetailModal").style.display = "block";
    } catch (e) {
        showCustomAlert("Lỗi khi lấy thông tin chi tiết: " + e.message, "Lỗi Hệ Thống", true);
    }
}

// 2. Mở Modal Sửa User (Changes)
window.openEditUserModal = async function(id) {
    try {
        const user = await fetchAPI(`/users/${id}`);
        
        document.getElementById("editUserId").value = user.userId;
        document.getElementById("editUsername").value = user.username || "";
        document.getElementById("editEmail").value = user.email || "";
        document.getElementById("editConsoleId").value = user.consoleLoginId || "";
        
        const roleSelect = document.getElementById("editRole");
        if(user.roleName === "ADMIN") roleSelect.value = "1";
        else if(user.roleName === "General Users") roleSelect.value = "2";
        else roleSelect.value = "2";

        document.getElementById("editAuditor").value = user.auditorFlag ? "true" : "false";

        document.getElementById("userEditModal").style.display = "block";
    } catch (e) {
        showCustomAlert("Lỗi khi lấy thông tin để sửa: " + e.message, "Lỗi Hệ Thống", true);
    }
}

// 3. Gửi Request Sửa User (Submit Update)
window.submitEditUser = async function() {
    const id = document.getElementById("editUserId").value;
    
    const payload = {
        Username: document.getElementById("editUsername").value,
        Email: document.getElementById("editEmail").value,
        ConsoleLoginId: document.getElementById("editConsoleId").value,
        RoleId: parseInt(document.getElementById("editRole").value),
        AuditorFlag: document.getElementById("editAuditor").value === "true"
    };

    try {
        await fetchAPI(`/users/${id}`, {
            method: "PUT",
            body: JSON.stringify(payload)
        });
        
        showCustomAlert("Cập nhật thông tin User thành công!", "Thành công");
        closeModal("userEditModal");
        loadUserData(); 
    } catch (e) {
        showCustomAlert("Lỗi khi cập nhật User: " + e.message, "Lỗi Hệ Thống", true);
    }
}

/* =========================================================================
   CÁC HÀM TIỆN ÍCH: CUSTOM CONFIRM VÀ CUSTOM ALERT
========================================================================= */

// Hàm thay thế window.alert()
window.showCustomAlert = function(message, title = "Notification", isError = false) {
    document.getElementById("alertMessage").innerText = message;
    document.getElementById("alertTitle").innerText = title;
    
    // Đổi màu header nếu là lỗi
    const header = document.querySelector("#customAlertModal .modal-header");
    if(isError) {
        header.style.backgroundColor = "#d32f2f"; // Đỏ
    } else {
        header.style.backgroundColor = "#000066"; // Xanh sậm mặc định
    }

    document.getElementById("customAlertModal").style.display = "block";
}

// Hàm thay thế window.confirm() (Dùng Promise để đợi người dùng bấm Yes/No)
window.showCustomConfirm = function(message, title = "Confirm Action") {
    return new Promise((resolve) => {
        document.getElementById("confirmMessage").innerText = message;
        document.getElementById("confirmTitle").innerText = title;
        document.getElementById("customConfirmModal").style.display = "block";

        // Bắt sự kiện nút Yes
        const btnYes = document.getElementById("btnConfirmYes");
        // Xóa các event listener cũ để tránh bị chạy lặp
        const newBtnYes = btnYes.cloneNode(true);
        btnYes.parentNode.replaceChild(newBtnYes, btnYes);

        newBtnYes.addEventListener("click", () => {
            closeModal("customConfirmModal");
            resolve(true); // Trả về true nếu bấm Yes
        });

        // Bắt sự kiện nút Cancel hoặc nút X (đã cấu hình sẵn ở hàm closeModal)
        const btnCancel = document.querySelector("#customConfirmModal .btn-cancel");
        const btnClose = document.querySelector("#customConfirmModal .close");
        
        const handleCancel = () => {
            closeModal("customConfirmModal");
            resolve(false); // Trả về false nếu bấm No
        };

        btnCancel.onclick = handleCancel;
        btnClose.onclick = handleCancel;
    });
}

/* =========================================================================
   HÀNH ĐỘNG BULK (HÀNG LOẠT) VÀ DOWNLOAD CSV - USER
========================================================================= */

window.toggleBulkDropdown = function(event, btnElement) {
    event.stopPropagation();
    const dropdown = btnElement.nextElementSibling;
    document.querySelectorAll('.dropdown-content').forEach(el => {
        if(el !== dropdown) el.classList.remove('show');
    });
    dropdown.classList.toggle("show");
}

window.selectAllUsers = function(isSelect) {
    document.querySelectorAll('.user-checkbox').forEach(cb => cb.checked = isSelect); 
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
}

window.getSelectedUserIds = function() {
    return Array.from(document.querySelectorAll('.user-checkbox:checked')).map(cb => parseInt(cb.value));
}

window.downloadSelectedUsers = function() {
    const selectedIds = getSelectedUserIds();
    if (selectedIds.length === 0) {
        showCustomAlert("Vui lòng chọn ít nhất 1 người dùng để tải xuống!", "Thông báo");
        return;
    }
    const selectedData = userData.filter(u => selectedIds.includes(u.userId));
    exportToCSV(selectedData, 'Selected_Users.csv');
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
}

window.downloadAllUsers = function() {
    if (userData.length === 0) {
        showCustomAlert("Không có dữ liệu để tải xuống!", "Thông báo");
        return;
    }
    exportToCSV(userData, 'All_Users.csv');
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
}

// Hàm dùng chung để xuất file CSV
if (typeof window.exportToCSV !== "function") {
    window.exportToCSV = function(dataArray, filename) {
        if (!dataArray || !dataArray.length) return;
        const headers = Object.keys(dataArray[0]);
        const csvRows = [headers.join(',')];
        for (const row of dataArray) {
            csvRows.push(headers.map(header => `"${String(row[header] || '').replace(/"/g, '""')}"`).join(','));
        }
        const blob = new Blob(['\uFEFF' + csvRows.join('\n')], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
}

/* =========================================================================
   THÊM MỚI NGƯỜI DÙNG (ADD USER)
========================================================================= */

// Hàm tải danh sách Phòng ban động từ DB
window.loadDepartmentOptionsForUser = async function(selectId) {
    try {
        const res = await fetchAPI("/departments?Page=1&PageSize=500");
        const depts = res.data || [];
        let html = '<option value="">-- Select Department --</option>';
        
        depts.forEach(d => {
            html += `<option value="${d.departmentId}">${d.departmentCode} - ${d.departmentName}</option>`;
        });
        
        const el = document.getElementById(selectId);
        if (el) el.innerHTML = html;
    } catch (e) {
        console.error("Lỗi tải danh sách Departments:", e);
    }
}

// Hàm xử lý mở giao diện
window.openAddUserModal = async function() {
    // 1. Xóa sạch dữ liệu cũ trên Form
    document.getElementById("addUCode").value = "";
    document.getElementById("addUName").value = "";
    document.getElementById("addUEmail").value = "";
    document.getElementById("addUPassword").value = "";
    document.getElementById("addUConsole").value = "";
    document.getElementById("addUSystem").value = "";
    document.getElementById("addURole").value = "2"; // Mặc định là General User
    
    // 2. Load danh sách phòng ban
    await loadDepartmentOptionsForUser("addUDept");
    
    // 3. Hiển thị Form
    document.getElementById("addUserModal").style.display = "block";
}

// Hàm gửi API tạo User mới
window.submitAddUser = async function() {
    // 1. Thu thập Payload
    const payload = {
        UserCode: document.getElementById("addUCode").value.trim(),
        Username: document.getElementById("addUName").value.trim(),
        UsernameAlphabet: "", // Không bắt buộc
        Email: document.getElementById("addUEmail").value.trim(),
        PasswordHash: document.getElementById("addUPassword").value.trim(),
        ConsoleLoginId: document.getElementById("addUConsole").value.trim(),
        SystemLoginId: document.getElementById("addUSystem").value.trim(),
        PrimaryDepartmentId: document.getElementById("addUDept").value ? parseInt(document.getElementById("addUDept").value) : null,
        RoleId: parseInt(document.getElementById("addURole").value)
    };

    // 2. Kiểm tra các trường Must (bắt buộc)
    if (!payload.UserCode || !payload.Username || !payload.Email || !payload.PasswordHash) {
        showCustomAlert("Vui lòng điền đầy đủ các thông tin bắt buộc (Must)!", "Cảnh báo", true);
        return;
    }

    // 3. Xin xác nhận từ người dùng
    const isConfirmed = await showCustomConfirm("Bạn có chắc chắn muốn tạo người dùng này?", "Xác nhận");
    if (!isConfirmed) return;

    // 4. Gửi Request
    try {
        await fetchAPI("/users", {
            method: "POST",
            body: JSON.stringify(payload)
        });
        
        showCustomAlert("Tạo tài khoản người dùng mới thành công!", "Thành công");
        closeModal("addUserModal");
        loadUserData(); // Tự động load lại bảng danh sách
    } catch (error) {
        showCustomAlert("Lỗi khi tạo User (Có thể trùng Email hoặc User Code): " + error.message, "Lỗi Hệ Thống", true);
    }
}