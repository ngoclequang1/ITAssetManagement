/* =========================================================================
   department-list.js  –  Department Search, List, và Detail Modal
   Bao gồm: xem chi tiết, assign user, remove user khỏi phòng ban
========================================================================= */

var deptData        = [];
var deptTotal       = 0;
var deptCurrentPage = 1;
var deptPageSize    = 10;

// ─────────────────────────────────────────────────────────────
// INIT
// ─────────────────────────────────────────────────────────────
window.initDepartmentList = function () {
    loadDepartmentData();
    setupDeptDropdownBehavior();
};

// ─────────────────────────────────────────────────────────────
// LOAD & SEARCH
// ─────────────────────────────────────────────────────────────
async function loadDepartmentData() {
    const container = document.getElementById("deptListContainer");
    if (!container) return;
    container.innerHTML = "<div style='padding:20px; text-align:center;'>Loading departments...</div>";

    const keyword     = document.getElementById("filterDeptKeyword")?.value.trim()    || "";
    const viewObsolete= document.getElementById("filterDeptObsolete")?.checked || false;

    let params = new URLSearchParams();
    params.append("Page",     deptCurrentPage);
    params.append("PageSize", deptPageSize);
    if (keyword)      params.append("Keyword",     keyword);
    if (viewObsolete) params.append("ViewObsolete", "true");

    try {
        const res  = await fetchAPI(`/departments?${params}`);
        deptData   = res.data  || [];
        deptTotal  = res.total || 0;
        renderDepartmentList();
        renderDeptPagination();
    } catch (e) {
        container.innerHTML = `<div style="color:red; padding:20px;">Failed to load departments: ${e.message}</div>`;
    }
}

window.searchDepartments = function () {
    deptCurrentPage = 1;
    loadDepartmentData();
};

window.clearDeptSearch = function () {
    const kw = document.getElementById("filterDeptKeyword");
    const ob = document.getElementById("filterDeptObsolete");
    if (kw) kw.value   = "";
    if (ob) ob.checked = false;
};

window.resetDeptSearch = function () {
    clearDeptSearch();
    searchDepartments();
};

// ─────────────────────────────────────────────────────────────
// RENDER LIST
// ─────────────────────────────────────────────────────────────
function renderDepartmentList() {
    const container = document.getElementById("deptListContainer");
    if (!container) return;

    if (!deptData || deptData.length === 0) {
        container.innerHTML = "<div style='padding:20px; text-align:center;'>No departments found.</div>";
        return;
    }

    container.innerHTML = deptData.map(d => `
        <div class="dept-card" style="background:white; border:1px solid #ccc; margin-bottom:16px;">
            <div class="card-header" style="background:#000066; color:white; padding:6px 15px;
                 display:flex; justify-content:space-between; align-items:center; font-weight:bold;">
                <div style="display:flex; align-items:center; gap:10px;">
                    <span>${d.departmentCode || '-'}</span>
                    <span style="font-weight:normal; font-size:12px;">|</span>
                    <span>${d.departmentName || '-'}</span>
                    ${!d.isActive ? '<span style="background:#d32f2f; color:white; font-size:10px; padding:1px 6px; border-radius:3px; margin-left:8px;">Obsolete</span>' : ''}
                </div>
                <div style="display:flex; align-items:center; gap:10px;">
                    <div class="dropdown">
                        <button class="btn-menu" style="background:#e0e0e0; border:1px solid #999;
                                padding:2px 10px; cursor:pointer; font-weight:bold; color:#000;"
                                onclick="toggleDeptDropdown(event, ${d.departmentId})">Menu</button>
                        <div id="dept-dropdown-${d.departmentId}" class="dropdown-content"
                             style="display:none; position:absolute; right:0; background:#fff;
                                    min-width:200px; box-shadow:0 8px 16px rgba(0,0,0,0.2);
                                    border:1px solid #ccc; z-index:1000;">
                            <a onclick="viewDeptDetail(${d.departmentId})"
                               style="display:block; padding:8px 12px; cursor:pointer; font-size:12px; border-bottom:1px dashed #eee;">
                                ▶ Description (View Details)
                            </a>
                            <a onclick="openAssignUserModal(${d.departmentId}, '${(d.departmentName||'').replace(/'/g,"\\'")}' )"
                               style="display:block; padding:8px 12px; cursor:pointer; font-size:12px; border-bottom:1px dashed #eee;">
                                ▶ Assign User
                            </a>
                            <a onclick="openEditDeptModal(${d.departmentId})"
                               style="display:block; padding:8px 12px; cursor:pointer; font-size:12px; border-bottom:1px dashed #eee;">
                                ▶ Edit Department
                            </a>
                            <a onclick="toggleDeptStatus(${d.departmentId}, ${d.isActive})"
                               style="display:block; padding:8px 12px; cursor:pointer; font-size:12px; color:${d.isActive ? '#d32f2f' : '#1b5e20'};">
                                ▶ ${d.isActive ? 'Mark as Obsolete' : 'Reactivate'}
                            </a>
                        </div>
                    </div>
                </div>
            </div>

            <div style="display:grid; grid-template-columns:200px 1fr 200px 1fr;">
                <div style="padding:8px 12px; background:#e6f0ff; border-bottom:1px solid #e0e0e0; border-right:1px solid #e0e0e0; font-size:12px;">Department ID</div>
                <div style="padding:8px 12px; background:#fff; border-bottom:1px solid #e0e0e0; border-right:1px solid #e0e0e0; font-size:12px;">${d.departmentId}</div>
                <div style="padding:8px 12px; background:#e6f0ff; border-bottom:1px solid #e0e0e0; border-right:1px solid #e0e0e0; font-size:12px;">Parent Department</div>
                <div style="padding:8px 12px; background:#fff; border-bottom:1px solid #e0e0e0; font-size:12px;">${d.parentDepartmentCode || 'None (Root)'}</div>

                <div style="padding:8px 12px; background:#e6f0ff; border-right:1px solid #e0e0e0; font-size:12px;">Department Name</div>
                <div style="padding:8px 12px; background:#fff; border-right:1px solid #e0e0e0; font-size:12px;">${d.departmentName || '-'}</div>
                <div style="padding:8px 12px; background:#e6f0ff; border-right:1px solid #e0e0e0; font-size:12px;">Top Deployment</div>
                <div style="padding:8px 12px; background:#fff; font-size:12px;">${d.topDepartmentName || '-'}</div>
            </div>
        </div>
    `).join("");
}

// ─────────────────────────────────────────────────────────────
// PAGINATION
// ─────────────────────────────────────────────────────────────
function renderDeptPagination() {
    const totalPages = Math.ceil(deptTotal / deptPageSize) || 1;
    let btns = `<button onclick="changeDeptPage(1)"><<</button>
                <button onclick="changeDeptPage(${deptCurrentPage > 1 ? deptCurrentPage-1 : 1})"><</button>`;

    for (let i = 1; i <= totalPages; i++) {
        btns += `<button class="${i===deptCurrentPage?'active':''}"
                         onclick="changeDeptPage(${i})">${i}</button>`;
    }
    btns += `<button onclick="changeDeptPage(${deptCurrentPage<totalPages?deptCurrentPage+1:totalPages})">></button>
             <button onclick="changeDeptPage(${totalPages})">>></button>`;

    const startItem = deptTotal === 0 ? 0 : (deptCurrentPage - 1) * deptPageSize + 1;
    const endItem   = Math.min(deptCurrentPage * deptPageSize, deptTotal);

    ["deptPaginationTop", "deptPaginationBottom"].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.innerHTML = btns;
    });
    ["deptTotalTop", "deptTotalBottom"].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.innerText = deptTotal;
    });
    ["deptShowStartTop", "deptShowStartBottom"].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.innerText = startItem;
    });
    ["deptShowEndTop", "deptShowEndBottom"].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.innerText = endItem;
    });
}

window.changeDeptPage = function (page) {
    deptCurrentPage = page;
    loadDepartmentData();
};

// ─────────────────────────────────────────────────────────────
// DROPDOWN BEHAVIOR
// ─────────────────────────────────────────────────────────────
window.toggleDeptDropdown = function (event, id) {
    event.stopPropagation();
    const curr = document.getElementById(`dept-dropdown-${id}`);
    document.querySelectorAll('[id^="dept-dropdown-"]').forEach(el => {
        if (el !== curr) el.style.display = "none";
    });
    curr.style.display = curr.style.display === "block" ? "none" : "block";
};

function setupDeptDropdownBehavior() {
    window.addEventListener("click", () => {
        document.querySelectorAll('[id^="dept-dropdown-"]').forEach(el => {
            el.style.display = "none";
        });
    });
}

// ─────────────────────────────────────────────────────────────
// VIEW DEPARTMENT DETAIL (Description)
// ─────────────────────────────────────────────────────────────
window.viewDeptDetail = async function (deptId) {
    document.querySelectorAll('[id^="dept-dropdown-"]').forEach(el => el.style.display = "none");

    const modal = document.getElementById("deptDetailModal");
    if (!modal) { console.error("deptDetailModal not found"); return; }

    // Show loading
    document.getElementById("deptDetailBody").innerHTML = "<tr><td colspan='4' style='text-align:center;padding:20px;'>Loading...</td></tr>";
    document.getElementById("deptMembersBody").innerHTML = "<tr><td colspan='5' style='text-align:center;padding:20px;'>Loading...</td></tr>";
    modal.style.display = "block";

    try {
        const data = await fetchAPI(`/departments/${deptId}`);
        const dept = data.department;
        const members = data.members || [];

        // ── Info table ──
        document.getElementById("deptDetailBody").innerHTML = `
            <tr>
                <th style="width:25%; background:#e6f0ff; padding:8px; border:1px solid #ccc;">Department ID</th>
                <td style="padding:8px; border:1px solid #ccc;">${dept.departmentId}</td>
                <th style="width:25%; background:#e6f0ff; padding:8px; border:1px solid #ccc;">Deployment Name</th>
                <td style="padding:8px; border:1px solid #ccc;">${dept.deploymentName || '-'}</td>
            </tr>
            <tr>
                <th style="background:#e6f0ff; padding:8px; border:1px solid #ccc;">Department Code</th>
                <td style="padding:8px; border:1px solid #ccc;">${dept.departmentCode}</td>
                <th style="background:#e6f0ff; padding:8px; border:1px solid #ccc;">Top Deployment Name</th>
                <td style="padding:8px; border:1px solid #ccc;">${dept.topDeploymentName || '-'}</td>
            </tr>
            <tr>
                <th style="background:#e6f0ff; padding:8px; border:1px solid #ccc;">Department Name</th>
                <td style="padding:8px; border:1px solid #ccc;">${dept.departmentName || '-'}</td>
                <th style="background:#e6f0ff; padding:8px; border:1px solid #ccc;">Overall Deployment</th>
                <td style="padding:8px; border:1px solid #ccc;">${dept.overallDeployment ? 'Yes' : 'No'}</td>
            </tr>
            <tr>
                <th style="background:#e6f0ff; padding:8px; border:1px solid #ccc;">Parent Department Code</th>
                <td style="padding:8px; border:1px solid #ccc;">${dept.parentDepartmentCode || 'None (Root)'}</td>
                <th style="background:#e6f0ff; padding:8px; border:1px solid #ccc;">Is Kitting Department</th>
                <td style="padding:8px; border:1px solid #ccc;">${dept.isKittingDepartment ? 'Yes' : 'No'}</td>
            </tr>
        `;

        // ── Members table ──
        // Lưu deptId để dùng khi remove
        document.getElementById("deptDetailModal").dataset.deptId = deptId;

        if (!members || members.length === 0) {
            document.getElementById("deptMembersBody").innerHTML = `
                <tr>
                    <td colspan="5" style="text-align:center; padding:16px; color:#999; font-style:italic;">
                        No members found. Use "Assign User" from the Menu to add users to this department.
                    </td>
                </tr>`;
        } else {
            document.getElementById("deptMembersBody").innerHTML = members.map(m => `
                <tr>
                    <td style="padding:8px; border-bottom:1px solid #eee;">${m.userCode || '-'}</td>
                    <td style="padding:8px; border-bottom:1px solid #eee; font-weight:bold;">${m.username || '-'}</td>
                    <td style="padding:8px; border-bottom:1px solid #eee;">${m.email || '-'}</td>
                    <td style="padding:8px; border-bottom:1px solid #eee;">
                        <span style="background:${getRoleBadgeColor(m.roleName)}; color:white;
                              padding:2px 8px; border-radius:3px; font-size:11px; font-weight:bold;">
                            ${m.roleName || 'General Staff'}
                        </span>
                        ${m.isPrimaryAdmin ? '<span style="background:#f39c12; color:white; padding:2px 6px; border-radius:3px; font-size:10px; margin-left:4px;">Primary Admin</span>' : ''}
                    </td>
                    <td style="padding:8px; border-bottom:1px solid #eee; text-align:center;">
                        <button onclick="removeUserFromDept(${deptId}, ${m.userId}, '${(m.username||'').replace(/'/g,"\\'")}' )"
                                style="background:#d32f2f; color:white; border:none; padding:3px 10px;
                                       cursor:pointer; font-size:11px; border-radius:2px;">
                            Remove
                        </button>
                    </td>
                </tr>
            `).join("");
        }

    } catch (e) {
        document.getElementById("deptDetailBody").innerHTML =
            `<tr><td colspan="4" style="color:red; padding:12px;">Error: ${e.message}</td></tr>`;
        document.getElementById("deptMembersBody").innerHTML =
            `<tr><td colspan="5" style="color:red; padding:12px;">Error: ${e.message}</td></tr>`;
    }
};

function getRoleBadgeColor(roleName) {
    if (!roleName) return "#757575";
    const r = roleName.toLowerCase();
    if (r.includes("admin"))   return "#000066";
    if (r.includes("manager")) return "#1b5e20";
    if (r.includes("rep"))     return "#4a148c";
    return "#455a64";
}

// ─────────────────────────────────────────────────────────────
// REMOVE USER FROM DEPARTMENT
// ─────────────────────────────────────────────────────────────
window.removeUserFromDept = async function (deptId, userId, username) {
    const ok = await showDeptConfirm(
        `Remove "${username}" from this department?`,
        "Confirm Remove"
    );
    if (!ok) return;

    try {
        await fetchAPI(`/departments/${deptId}/remove-user/${userId}`, { method: "DELETE" });
        showDeptAlert(`"${username}" has been removed from the department.`, "Success");
        // Reload detail
        viewDeptDetail(deptId);
    } catch (e) {
        showDeptAlert("Error: " + e.message, "Error", true);
    }
};

// ─────────────────────────────────────────────────────────────
// ASSIGN USER MODAL
// ─────────────────────────────────────────────────────────────
window.openAssignUserModal = async function (deptId, deptName) {
    document.querySelectorAll('[id^="dept-dropdown-"]').forEach(el => el.style.display = "none");

    const modal = document.getElementById("deptAssignUserModal");
    if (!modal) { console.error("deptAssignUserModal not found"); return; }

    document.getElementById("assignDeptId").value           = deptId;
    document.getElementById("assignDeptNameLabel").innerText = deptName;
    document.getElementById("assignIsPrimaryAdmin").checked  = false;

    // Load available users (chưa thuộc dept này)
    const select = document.getElementById("assignUserId");
    select.innerHTML = "<option value=''>Loading users...</option>";
    modal.style.display = "block";

    try {
        const users = await fetchAPI(`/departments/${deptId}/available-users`);
        if (!users || users.length === 0) {
            select.innerHTML = "<option value=''>No available users to assign</option>";
        } else {
            select.innerHTML = "<option value=''>-- Select User --</option>" +
                users.map(u =>
                    `<option value="${u.userId}">
                        ${u.username} (${u.userCode || u.email || ''}) – ${u.roleName}
                        ${u.currentDepartment !== 'None' ? '[' + u.currentDepartment + ']' : ''}
                    </option>`
                ).join("");
        }
    } catch (e) {
        select.innerHTML = `<option value=''>Error loading users: ${e.message}</option>`;
    }
};

window.submitAssignUser = async function () {
    const deptId       = parseInt(document.getElementById("assignDeptId").value);
    const userId       = parseInt(document.getElementById("assignUserId").value);
    const isPrimary    = document.getElementById("assignIsPrimaryAdmin").checked;

    if (!userId) {
        showDeptAlert("Please select a user.", "Validation", true);
        return;
    }

    try {
        const res = await fetchAPI(`/departments/${deptId}/assign-user`, {
            method: "POST",
            body: JSON.stringify({ UserId: userId, IsPrimaryAdmin: isPrimary })
        });
        showDeptAlert(res.message || "User assigned successfully.", "Success");
        document.getElementById("deptAssignUserModal").style.display = "none";

        // Nếu detail modal đang mở thì reload luôn
        const detailModal = document.getElementById("deptDetailModal");
        if (detailModal && detailModal.style.display === "block" &&
            detailModal.dataset.deptId == deptId) {
            viewDeptDetail(deptId);
        }
    } catch (e) {
        showDeptAlert("Error: " + e.message, "Error", true);
    }
};

// ─────────────────────────────────────────────────────────────
// EDIT DEPARTMENT MODAL
// ─────────────────────────────────────────────────────────────
window.openEditDeptModal = async function (deptId) {
    document.querySelectorAll('[id^="dept-dropdown-"]').forEach(el => el.style.display = "none");

    const modal = document.getElementById("deptEditModal");
    if (!modal) { console.error("deptEditModal not found"); return; }

    try {
        const data = await fetchAPI(`/departments/${deptId}`);
        const dept = data.department;
        document.getElementById("editDeptId").value          = dept.departmentId;
        document.getElementById("editDeptName").value        = dept.departmentName || "";
        document.getElementById("editDeptCodeLabel").innerText = dept.departmentCode;
        modal.style.display = "block";
    } catch (e) {
        showDeptAlert("Error loading department: " + e.message, "Error", true);
    }
};

window.submitEditDept = async function () {
    const deptId   = parseInt(document.getElementById("editDeptId").value);
    const deptName = document.getElementById("editDeptName").value.trim();

    if (!deptName) {
        showDeptAlert("Department name is required.", "Validation", true);
        return;
    }

    try {
        await fetchAPI(`/departments/${deptId}`, {
            method: "PUT",
            body: JSON.stringify({ DepartmentName: deptName, ParentDepartmentId: null })
        });
        showDeptAlert("Department updated successfully.", "Success");
        document.getElementById("deptEditModal").style.display = "none";
        loadDepartmentData();
    } catch (e) {
        showDeptAlert("Error: " + e.message, "Error", true);
    }
};

// ─────────────────────────────────────────────────────────────
// TOGGLE DEPT STATUS (Active / Obsolete)
// ─────────────────────────────────────────────────────────────
window.toggleDeptStatus = async function (deptId, isCurrentlyActive) {
    document.querySelectorAll('[id^="dept-dropdown-"]').forEach(el => el.style.display = "none");

    const action = isCurrentlyActive ? "mark as Obsolete" : "reactivate";
    const ok = await showDeptConfirm(`Are you sure you want to ${action} this department?`, "Confirm");
    if (!ok) return;

    try {
        await fetchAPI(`/departments/${deptId}/status`, {
            method: "PUT",
            body: JSON.stringify({ IsActive: !isCurrentlyActive })
        });
        showDeptAlert(`Department ${isCurrentlyActive ? "deactivated" : "reactivated"} successfully.`, "Success");
        loadDepartmentData();
    } catch (e) {
        showDeptAlert("Error: " + e.message, "Error", true);
    }
};

// ─────────────────────────────────────────────────────────────
// MODAL UTILITIES
// ─────────────────────────────────────────────────────────────
window.closeDeptModal = function (modalId) {
    const el = document.getElementById(modalId);
    if (el) el.style.display = "none";
};

window.showDeptAlert = function (message, title = "Notification", isError = false) {
    // Dùng showCustomAlert của dashboard nếu có, fallback về alert()
    if (typeof showCustomAlert === "function") {
        showCustomAlert(message, title, isError);
    } else {
        alert(`${title}: ${message}`);
    }
};

window.showDeptConfirm = function (message, title = "Confirm") {
    if (typeof showCustomConfirm === "function") {
        return showCustomConfirm(message, title);
    }
    return Promise.resolve(confirm(`${title}\n\n${message}`));
};