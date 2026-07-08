const API = "http://localhost:5288/api";

const token = localStorage.getItem("token");
const role = localStorage.getItem("role");

// Current user ID - used for approval checks
const currentUserId = parseInt(localStorage.getItem("userId")) || 0;

if (!token) {
    window.location.href = "../Auth/login.html";
}

// ─── Permission helpers (mirrors permission-guard.js logic but
//     embedded here so dashboard works standalone) ───────────
var _dashPerm = {
    canView: true, canRequest: false, canApprove: false, canAdmin: false
};

async function _loadDashPermission() {
    if (!currentUserId) return;
    // FIX: dùng roleId đã được login.js lưu vào localStorage
    const rid = parseInt(localStorage.getItem("role")) || 2;
    try {
        const res = await fetchAPI(`/asset-request/permission/${currentUserId}`);
        _dashPerm = {
            canView:    !!res.canView,
            canRequest: !!res.canRequest,
            canApprove: !!res.canApprove,
            canAdmin:   !!res.canAdmin
        };
    } catch (e) {
        // Fallback dùng roleId từ localStorage
        console.warn("[Dashboard] Permission API failed, using roleId:", rid);
        _dashPerm = {
            canView:    true,
            canRequest: [1, 3, 4, 5].includes(rid),
            canApprove: [1, 4].includes(rid),
            canAdmin:   rid === 1
        };
    }
    window._dashPermLoaded = true; // FIX: đánh dấu đã load xong
    _applyDashSidebarPermission();
}

function _applyDashSidebarPermission() {
    // Pending Approvals link: only approvers see it
    document.querySelectorAll('[data-page="approvals"]').forEach(function (el) {
        el.style.display = _dashPerm.canApprove ? "" : "none";
    });

    const switchWrapper = document.getElementById("menuSwitchWrapper");
    const savedGroup = localStorage.getItem("menuSwitchGroup") || "asset";

    if (_dashPerm.canAdmin) {
        if (switchWrapper) switchWrapper.style.display = "flex";
        const sel = document.getElementById("menuSwitchSelect");
        if (sel) sel.value = savedGroup;
        applyMenuGroup(savedGroup);
    } else {
        if (switchWrapper) switchWrapper.style.display = "none";
        applyMenuGroup("asset"); // non-admin luôn chỉ thấy nhóm Asset
    }
}

/* =============================
   BIẾN TOÀN CỤC CHO PHÂN TRANG TẠI DASHBOARD
============================= */
const DASH_PAGE_SIZE = 10; 

let dashApprData = [];
let dashApprPage = 1;

let dashHwData = [];
let dashHwPage = 1;

let dashSwData = [];
let dashSwPage = 1;

let dashReqData = [];
let dashReqPage = 1;

/* =============================
   INIT
============================= */
document.addEventListener("DOMContentLoaded", async () => {
    setupMenu();
    _renderHeaderInfo();
    _loadDashPermission(); // non-blocking
    await loadOverview();
});
/* =============================
   MENU
============================= */
function setupMenu() {
    document.querySelectorAll(".sidebar a").forEach(link => {
        link.addEventListener("click", async () => {
            const page = link.dataset.page;

            switch (page) {
                case "overview": await loadOverview(); break;
                case "hardware": await loadHardware(); break;
                case "software": await loadSoftware(); break;
                case "request": await loadRequests(); break;
                case "approvals": await loadApprovals(); break;
                case "software-management": await loadSoftwareManagement(); break;
                case "hardware-management": await loadHardwareManagement(); break;
                case "license-management": await loadLicenseManagement(); break;
                case "users": await loadUsers(); break;
                case "departments": await loadDepartments(); break;
                case "import-management": await loadImportManagement(); break;
            }
        });
    });

    document.getElementById("logoutBtn").onclick = () => {
        localStorage.clear();
        window.location.href = "../Auth/login.html";
    };
}

/* =============================
   HÀM TIỆN ÍCH: TẠO GIAO DIỆN PHÂN TRANG
============================= */
function renderPagination(totalItems, currentPage, pageSize, changePageFuncName) {
    const totalPages = Math.ceil(totalItems / pageSize) || 1;
    
    let html = `<div style="margin-top: 15px; text-align: center; display: flex; justify-content: center; gap: 5px;">`;
    
    html += `<button onclick="${changePageFuncName}(1)" ${currentPage === 1 ? 'disabled' : ''} style="padding: 5px 10px; cursor: pointer;"><<</button>`;
    html += `<button onclick="${changePageFuncName}(${currentPage - 1})" ${currentPage === 1 ? 'disabled' : ''} style="padding: 5px 10px; cursor: pointer;"><</button>`;
    
    for(let i = 1; i <= totalPages; i++) {
        let activeStyle = currentPage === i ? 'background-color: #000066; color: white;' : 'background-color: #f0f0f0;';
        html += `<button onclick="${changePageFuncName}(${i})" style="padding: 5px 10px; border: 1px solid #ccc; cursor: pointer; ${activeStyle}">${i}</button>`;
    }
    
    html += `<button onclick="${changePageFuncName}(${currentPage + 1})" ${currentPage === totalPages ? 'disabled' : ''} style="padding: 5px 10px; cursor: pointer;">></button>`;
    html += `<button onclick="${changePageFuncName}(${totalPages})" ${currentPage === totalPages ? 'disabled' : ''} style="padding: 5px 10px; cursor: pointer;">>></button>`;
    
    html += `</div>`;
    return html;
}

/* =============================
   DASHBOARD OVERVIEW (CÓ BIỂU ĐỒ PIE CHART)
============================= */
async function loadOverview() {
    try {
        setTitle("Dashboard Overview");

        // Gọi API lấy dữ liệu
        const [hardware, software, requests, usersRes, deptsRes, myPending] = await Promise.all([
            fetchAPI("/hardware"),
            fetchAPI("/software"),
            fetchAPI("/request"),
            fetchAPI("/users?Page=1&PageSize=1").catch(() => ({ total: 0 })), 
            fetchAPI("/departments?Page=1&PageSize=1").catch(() => ({ total: 0 })),
            fetchAPI(`/request/pending/${currentUserId}`).catch(() => ([]))
        ]);

        // 1. Phân tích Dữ liệu Phần cứng (Hardware)
        const hwTotal = hardware.length;
        const hwAvailable = hardware.filter(h => h.status === 'Available').length;
        const hwInUse = hardware.filter(h => h.status === 'In Use').length;
        const hwBroken = hardware.filter(h => h.status === 'Broken' || h.status === 'inactive').length;

        // 2. Phân tích Dữ liệu Yêu cầu (Requests)
        const reqTotal = requests.length;
        const reqPending = requests.filter(r => r.status === 'Pending').length;
        const reqApproved = requests.filter(r => r.status === 'Approved').length;
        const reqRejected = requests.filter(r => r.status === 'Rejected').length;

        // 3. Tổng hợp dữ liệu khác
        const swTotal = software.length || software.data?.length || 0;
        const userTotal = usersRes.total || 0;
        const deptTotal = deptsRes.total || 0;
        const myPendingCount = myPending.length || 0;

        // 4. Vẽ giao diện HTML với CSS Grid và vùng chứa thẻ Canvas
        document.getElementById("content").innerHTML = `
            <style>
                .dash-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; margin-bottom: 25px; }
                .kpi-card { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.05); border-left: 5px solid #000066; display: flex; flex-direction: column; justify-content: space-between; }
                .kpi-card.warning { border-left-color: #f39c12; background: #fffdfa; }
                
                .kpi-title { font-size: 13px; color: #777; text-transform: uppercase; font-weight: bold; margin-bottom: 10px; }
                .kpi-value { font-size: 28px; font-weight: bold; color: #333; margin: 0; }
                
                .chart-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }
                .chart-panel { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.05); }
                .chart-panel h3 { margin-top: 0; font-size: 16px; border-bottom: 1px solid #eee; padding-bottom: 10px; color: #000066; }
                
                .stat-row { display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px dashed #f0f0f0; font-size: 14px; }
                .stat-row:last-child { border-bottom: none; }
                .stat-dot { display: inline-block; width: 10px; height: 10px; border-radius: 50%; margin-right: 8px; }
                
                .canvas-container { height: 250px; display: flex; justify-content: center; margin-top: 15px; }
            </style>

            <div class="dash-grid">
                <div class="kpi-card">
                    <div class="kpi-title">Total Hardware</div>
                    <div class="kpi-value">${hwTotal}</div>
                </div>
                <div class="kpi-card">
                    <div class="kpi-title">Total Software</div>
                    <div class="kpi-value">${swTotal}</div>
                </div>
                <div class="kpi-card">
                    <div class="kpi-title">Total Users</div>
                    <div class="kpi-value">${userTotal}</div>
                </div>
                <div class="kpi-card warning" style="cursor: pointer;" onclick="loadApprovals()">
                    <div class="kpi-title">My Pending Approvals</div>
                    <div class="kpi-value" style="color: #f39c12;">${myPendingCount}</div>
                    <div style="font-size: 11px; color: #999; margin-top: 5px;">Click to review ▶</div>
                </div>
            </div>

            <div class="chart-grid">
                <div class="chart-panel">
                    <h3>Hardware Breakdown</h3>
                    <div class="stat-row">
                        <span><span class="stat-dot" style="background:#28a745;"></span>In Use (Đang sử dụng)</span>
                        <strong>${hwInUse}</strong>
                    </div>
                    <div class="stat-row">
                        <span><span class="stat-dot" style="background:#4a86e8;"></span>Available (Trong kho)</span>
                        <strong>${hwAvailable}</strong>
                    </div>
                    <div class="stat-row">
                        <span><span class="stat-dot" style="background:#d32f2f;"></span>Broken/Inactive (Hỏng)</span>
                        <strong style="color:#d32f2f;">${hwBroken}</strong>
                    </div>
                    <div class="canvas-container">
                        <canvas id="hwChart"></canvas>
                    </div>
                </div>

                <div class="chart-panel">
                    <h3>System Requests</h3>
                    <div class="stat-row">
                        <span><span class="stat-dot" style="background:#f39c12;"></span>Pending (Đang chờ duyệt)</span>
                        <strong>${reqPending}</strong>
                    </div>
                    <div class="stat-row">
                        <span><span class="stat-dot" style="background:#28a745;"></span>Approved (Đã duyệt)</span>
                        <strong>${reqApproved}</strong>
                    </div>
                    <div class="stat-row">
                        <span><span class="stat-dot" style="background:#d32f2f;"></span>Rejected (Đã từ chối)</span>
                        <strong>${reqRejected}</strong>
                    </div>
                    <div class="canvas-container">
                        <canvas id="reqChart"></canvas>
                    </div>
                </div>
            </div>
        `;

        // ============================================
        // 5. VẼ BIỂU ĐỒ BẰNG CHART.JS
        // ============================================
        
        // Cấu hình chung để biểu đồ hiển thị đẹp và có tính năng hover
        const commonOptions = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'right' },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            let label = context.label || '';
                            if (label) label += ': ';
                            if (context.parsed !== null) label += context.parsed + ' items';
                            return label;
                        }
                    }
                }
            }
        };

        // Vẽ biểu đồ Hardware
        const hwCtx = document.getElementById('hwChart').getContext('2d');
        new Chart(hwCtx, {
            type: 'doughnut', 
            data: {
                labels: ['In Use', 'Available', 'Broken/Inactive'],
                datasets: [{
                    data: [hwInUse, hwAvailable, hwBroken],
                    backgroundColor: ['#28a745', '#4a86e8', '#d32f2f'], 
                    borderWidth: 1
                }]
            },
            options: commonOptions
        });

        // Vẽ biểu đồ Requests
        const reqCtx = document.getElementById('reqChart').getContext('2d');
        new Chart(reqCtx, {
            type: 'doughnut',
            data: {
                labels: ['Pending', 'Approved', 'Rejected'],
                datasets: [{
                    data: [reqPending, reqApproved, reqRejected],
                    backgroundColor: ['#f39c12', '#28a745', '#d32f2f'], 
                    borderWidth: 1
                }]
            },
            options: commonOptions
        });

    } catch (err) {
        console.error(err);
        document.getElementById("content").innerHTML =
            `<div class="card" style="color: red; padding: 20px;">Lỗi khi tải dữ liệu Overview: ${err.message}</div>`;
    }
}

/* =============================
   PENDING APPROVALS (NEW)
============================= */
async function loadApprovals() {
    if (!_dashPerm.canApprove) {
        document.getElementById("content").innerHTML =
            `<div class="card" style="padding:30px; text-align:center; color:#757575;">
                <h3>Access Denied</h3>
                <p style="margin-top:10px;">You do not have permission to view pending approvals.<br>This section requires Administrator or ADMIN role.</p>
             </div>`;
        return;
    }
    try {
        setTitle("Pending Approvals");
        dashApprData = await fetchAPI(`/request/pending/${currentUserId}`);
        dashApprPage = 1;
        renderApprovals();
    } catch (err) {
        console.error(err);
        document.getElementById("content").innerHTML = `<div class="card" style="color:red;">Error loading approvals: ${err.message}</div>`;
    }
}

function renderApprovals() {
    if(dashApprData.length === 0) {
        document.getElementById("content").innerHTML = `
            <div class="card" style="text-align: center; padding: 40px; color: #555;">
                <h3>Bạn không có yêu cầu nào đang chờ phê duyệt.</h3>
            </div>`;
        return;
    }

    const start = (dashApprPage - 1) * DASH_PAGE_SIZE;
    const pageData = dashApprData.slice(start, start + DASH_PAGE_SIZE);

    let html = `
        <div class="card">
            <table class="approvals-table" style="width: 100%; border-collapse: collapse;">
                <tr style="background-color: #f0f4f8;">
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Req ID</th>
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Type</th>
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Description</th>
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Level</th>
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Date</th>
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Action</th>
                </tr>
                ${pageData.map(x => `
                    <tr>
                        <td style="padding: 10px; border: 1px solid #ddd;">${x.requestId}</td>
                        <td style="padding: 10px; border: 1px solid #ddd;"><span style="background: #e6f0ff; padding: 2px 8px; border-radius: 4px; font-size: 12px; border: 1px solid #b3d4ff;">${x.type}</span></td>
                        <td style="padding: 10px; border: 1px solid #ddd;">${x.description || "-"}</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">Level ${x.approvalLevel}</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">${new Date(x.createdAt).toLocaleDateString()}</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">
                            <button style="background-color: #4a86e8; color: white; border: none; padding: 5px 10px; cursor: pointer; border-radius: 3px;" onclick="viewApprovalDetail(${x.requestId})">Review</button>
                        </td>
                    </tr>
                `).join("")}
            </table>
            ${renderPagination(dashApprData.length, dashApprPage, DASH_PAGE_SIZE, 'changeApprPage')}
        </div>
    `;
    document.getElementById("content").innerHTML = html;
}

window.changeApprPage = function(page) {
    dashApprPage = page;
    renderApprovals();
}

// Bật Modal xem chi tiết Approval
window.viewApprovalDetail = async function(reqId) {
    try {
        const data = await fetchAPI(`/request/${reqId}`);
        
        document.getElementById("apprReqId").value = reqId;
        document.getElementById("apprId").innerText = data.requestId;
        document.getElementById("apprType").innerText = data.type;
        document.getElementById("apprDesc").innerText = data.requestDescription || "-";
        document.getElementById("apprRemarks").value = ""; 

        let detailsHtml = "";
        if (data.details && data.details.length > 0) {
            data.details.forEach(d => {
                detailsHtml += `<tr>
                    <td style="padding: 8px; border: 1px solid #ddd;">${d.fieldName}</td>
                    <td style="padding: 8px; border: 1px solid #ddd;">${d.oldValue || "-"}</td>
                    <td style="padding: 8px; border: 1px solid #ddd; color: #000066; font-weight: bold;">${d.newValue || "-"}</td>
                </tr>`;
            });
        } else {
            detailsHtml = `<tr><td colspan="3" style="text-align:center; padding: 8px;">No specific changes attached.</td></tr>`;
        }
        document.getElementById("apprDetailsBody").innerHTML = detailsHtml;

        document.getElementById("approvalDetailModal").style.display = "block";
    } catch (e) {
        alert("Lỗi tải chi tiết: " + e.message);
    }
}

// Gửi lệnh Approve (Có cơ chế chống Spam Click)
window.submitApprove = async function() {
    const reqId = document.getElementById("apprReqId").value;
    const remarks = document.getElementById("apprRemarks").value;
    const btnApprove = document.getElementById("btnApproveReq");
    const btnReject = document.getElementById("btnRejectReq");
    
    if(btnApprove) { btnApprove.disabled = true; btnApprove.innerText = "Processing..."; }
    if(btnReject) { btnReject.disabled = true; }

    try {
        await fetchAPI(`/request/${reqId}/approve`, {
            method: "PUT",
            body: JSON.stringify({ ApproverId: currentUserId, Remarks: remarks })
        });
        document.getElementById("approvalDetailModal").style.display = "none";
        showCustomAlert("Đã CHẤP THUẬN yêu cầu thành công!", "Thành công");
        loadApprovals(); 
    } catch (e) {
        showCustomAlert("Lỗi khi approve: " + e.message, "Lỗi Hệ Thống", true);
    } finally {
        if(btnApprove) { btnApprove.disabled = false; btnApprove.innerText = "Approve"; }
        if(btnReject) { btnReject.disabled = false; }
    }
}

// Gửi lệnh Reject (Có cơ chế chống Spam Click)
window.submitReject = async function() {
    const reqId = document.getElementById("apprReqId").value;
    const remarks = document.getElementById("apprRemarks").value;
    const btnApprove = document.getElementById("btnApproveReq");
    const btnReject = document.getElementById("btnRejectReq");
    
    if(!remarks.trim()) {
        showCustomAlert("Vui lòng nhập lý do (Remarks) khi Từ chối!", "Cảnh báo", true);
        return;
    }

    if(btnApprove) { btnApprove.disabled = true; }
    if(btnReject) { btnReject.disabled = true; btnReject.innerText = "Processing..."; }

    try {
        await fetchAPI(`/request/${reqId}/reject`, {
            method: "PUT",
            body: JSON.stringify({ ApproverId: currentUserId, Remarks: remarks })
        });
        document.getElementById("approvalDetailModal").style.display = "none";
        showCustomAlert("Đã TỪ CHỐI yêu cầu thành công!", "Thành công");
        loadApprovals(); 
    } catch (e) {
        showCustomAlert("Lỗi khi reject: " + e.message, "Lỗi Hệ Thống", true);
    } finally {
        if(btnApprove) { btnApprove.disabled = false; }
        if(btnReject) { btnReject.disabled = false; btnReject.innerText = "Reject"; }
    }
}

/* =============================
   HARDWARE (STATIC)
============================= */
async function loadHardware() {
    try {
        setTitle("Hardware");
        dashHwData = await fetchAPI("/hardware");
        dashHwPage = 1;
        renderDashHardware();
    } catch (err) { console.error(err); }
}

function renderDashHardware() {
    const start = (dashHwPage - 1) * DASH_PAGE_SIZE;
    const pageData = dashHwData.slice(start, start + DASH_PAGE_SIZE);

    let html = `
        <div class="card">
            <table style="width: 100%; border-collapse: collapse;">
                <tr style="background-color: #f0f4f8;">
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">ID</th>
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Name</th>
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Model</th>
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Status</th>
                </tr>
                ${pageData.map(x => `
                    <tr>
                        <td style="padding: 10px; border: 1px solid #ddd;">${x.assetId}</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">${x.assetName}</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">${x.model}</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">${x.status}</td>
                    </tr>
                `).join("")}
            </table>
            ${renderPagination(dashHwData.length, dashHwPage, DASH_PAGE_SIZE, 'changeHwPage')}
        </div>
    `;
    document.getElementById("content").innerHTML = html;
}

window.changeHwPage = function(page) {
    dashHwPage = page;
    renderDashHardware();
}

/* =============================
   SOFTWARE (STATIC)
============================= */
async function loadSoftware() {
    try {
        setTitle("Software");
        dashSwData = await fetchAPI("/software");
        dashSwPage = 1;
        renderDashSoftware();
    } catch (err) { console.error(err); }
}

function renderDashSoftware() {
    const start = (dashSwPage - 1) * DASH_PAGE_SIZE;
    const pageData = dashSwData.slice(start, start + DASH_PAGE_SIZE);

    let html = `
        <div class="card">
            <table style="width: 100%; border-collapse: collapse;">
                <tr style="background-color: #f0f4f8;">
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">ID</th>
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Name</th>
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Version</th>
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Group</th>
                </tr>
                ${pageData.map(x => `
                    <tr>
                        <td style="padding: 10px; border: 1px solid #ddd;">${x.softwareId}</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">${x.softwareName}</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">${x.softwareVersion}</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">${x.groupId || "-"}</td>
                    </tr>
                `).join("")}
            </table>
            ${renderPagination(dashSwData.length, dashSwPage, DASH_PAGE_SIZE, 'changeSwPage')}
        </div>
    `;
    document.getElementById("content").innerHTML = html;
}

window.changeSwPage = function(page) {
    dashSwPage = page;
    renderDashSoftware();
}

/* =============================
   REQUEST (STATIC)
============================= */
async function loadRequests() {
    try {
        setTitle("Requests");
        dashReqData = await fetchAPI("/request");
        dashReqPage = 1;
        renderDashRequests();
    } catch (err) { console.error(err); }
}

function renderDashRequests() {
    const start = (dashReqPage - 1) * DASH_PAGE_SIZE;
    const pageData = dashReqData.slice(start, start + DASH_PAGE_SIZE);

    let html = `
        <div class="card">
            <table style="width: 100%; border-collapse: collapse;">
                <tr style="background-color: #f0f4f8;">
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">ID</th>
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Type</th>
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Status</th>
                    <th style="padding: 10px; border: 1px solid #ddd; text-align: left;">Description</th>
                </tr>
                ${pageData.map(x => `
                    <tr>
                        <td style="padding: 10px; border: 1px solid #ddd;">${x.requestId}</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">${x.type}</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">${x.status}</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">${x.requestDescription || ""}</td>
                    </tr>
                `).join("")}
            </table>
            ${renderPagination(dashReqData.length, dashReqPage, DASH_PAGE_SIZE, 'changeReqPage')}
        </div>
    `;
    document.getElementById("content").innerHTML = html;
}

window.changeReqPage = function(page) {
    dashReqPage = page;
    renderDashRequests();
}

/* =========================================================
   DYNAMIC MODULES (Sửa lỗi Script Loading)
========================================================= */

function reloadScript(scriptSrc, initFunction) {
    const oldScript = document.querySelector(`script[src="${scriptSrc}"]`);
    if (oldScript) {
        oldScript.remove();
    }

    const script = document.createElement("script");
    script.src = scriptSrc;
    script.onload = () => { 
        if (typeof window[initFunction] === "function") {
            window[initFunction](); 
        } 
    };
    document.body.appendChild(script);
}

async function loadSoftwareManagement() {
    try {
        setTitle("Software Management");
        const res = await fetch("../../html/Software-Management/software-management.html");
        document.getElementById("content").innerHTML = await res.text();
        reloadScript("../../js/SoftwareManagement/software-management.js", "initSoftwareManagement");
    } catch (err) { console.error(err); }
}

async function loadHardwareManagement() {
    try {
        setTitle("Hardware Management");
        const res = await fetch("../../html/Hardware-Management/hardware-management.html");
        document.getElementById("content").innerHTML = await res.text();
        reloadScript("../../js/HardwareManagement/hardware-management.js", "initHardwareManagement");
    } catch (err) { console.error(err); }
}

// FIX: helper đảm bảo permission đã được load trước khi kiểm tra
async function _ensurePermLoaded() {
    // Nếu permission vẫn là default (userId = 0) thì chờ load
    if (window._dashPermLoaded) return;
    await _loadDashPermission();
}

async function loadUsers() {
    // FIX: đảm bảo permission đã load xong thay vì kiểm tra biến chưa set
    await _ensurePermLoaded();
    if (!_dashPerm.canAdmin) {
        document.getElementById("content").innerHTML =
            `<div class="card" style="padding:30px; text-align:center; color:#757575;">
                <h3>Access Denied</h3>
                <p style="margin-top:10px;">User management requires ADMIN access.</p>
             </div>`;
        return;
    }
    try {
        setTitle("User Search and List");
        const res = await fetch("../../html/User/user-list.html");
        if (!res.ok) throw new Error("Không tìm thấy file user-list.html");
        document.getElementById("content").innerHTML = await res.text();
        reloadScript("../../js/User/user-list.js", "initUserList");
    } catch (err) {
        document.getElementById("content").innerHTML = `<div style="padding: 20px; color: red;">Lỗi khi tải trang User List: ${err.message}</div>`;
    }
}

async function loadDepartments() {
    // FIX: đảm bảo permission đã load xong
    await _ensurePermLoaded();
    if (!_dashPerm.canAdmin) {
        document.getElementById("content").innerHTML =
            `<div class="card" style="padding:30px; text-align:center; color:#757575;">
                <h3>Access Denied</h3>
                <p style="margin-top:10px;">Department management requires ADMIN access.</p>
             </div>`;
        return;
    }
    try {
        setTitle("Department Search & List");
        const res = await fetch("../../html/Department/department-list.html");
        if (!res.ok) throw new Error("Không tìm thấy file department-list.html");
        document.getElementById("content").innerHTML = await res.text();
        reloadScript("../../js/Department/department-list.js", "initDepartmentList");
    } catch (err) {
        document.getElementById("content").innerHTML = `<div style="padding: 20px; color: red;">Lỗi khi tải trang Department List: ${err.message}</div>`;
    }
}

/* =============================
   HELPERS
============================= */
function setTitle(title) {
    document.getElementById("pageTitle").innerText = title;
}

/* =============================
   FETCH API (Upgraded: Tự động nhận diện JSON hoặc Text)
============================= */
async function fetchAPI(endpoint, options = {}) {
    const res = await fetch(API + endpoint, {
        headers: {
            "Content-Type": "application/json",
            "Authorization": "Bearer " + token
        },
        ...options
    });

    if (!res.ok) {
        const text = await res.text();
        console.error("API Error:", text);
        throw new Error(text);
    }

    const contentType = res.headers.get("content-type");
    if (contentType && contentType.includes("application/json")) {
        return await res.json(); 
    } else {
        return await res.text(); 
    }
} 

/* =========================================================
   CUSTOM ALERTS DÀNH CHO DASHBOARD
========================================================= */
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

window.closeModal = function(modalId) {
    const modal = document.getElementById(modalId);
    if(modal) modal.style.display = "none";
}


async function loadLicenseManagement() {
    setTitle("License Management");
    const res = await fetch("../../html/LicenseManagement/license-management.html");
    document.getElementById("content").innerHTML = await res.text();
    reloadScript("../../js/LicenseManagement/license-management.js", "initLicenseManagement");
}

async function loadImportManagement() {
    try {
        setTitle("Import Data");
        const res = await fetch("../../html/Import/import-management.html");
        if (!res.ok) throw new Error("Cannot find import-management.html");
        document.getElementById("content").innerHTML = await res.text();
        reloadScript("../../js/Import/import-management.js", "initImportManagement");
    } catch (err) {
        document.getElementById("content").innerHTML =
            `<div style="padding: 20px; color: red;">Error loading Import page: ${err.message}</div>`;
    }
}

/* =============================
   MENU SWITCH (Asset Management / User Management)
============================= */
function applyMenuGroup(group) {
    document.querySelectorAll('[data-group]').forEach(el => {
        const g = el.getAttribute('data-group');
        if (g === 'common') { el.style.display = ''; return; }
        el.style.display = (g === group) ? '' : 'none';
    });
    localStorage.setItem('menuSwitchGroup', group);
}

window.onMenuSwitchChange = function (value) {
    applyMenuGroup(value);
};

function _renderHeaderInfo() {
    const username = localStorage.getItem('username') || 'User';
    const roleName = localStorage.getItem('roleName') || '';
    const userEl = document.getElementById('headerUserName');
    if (userEl) userEl.innerText = roleName ? `${username}, ${roleName}` : username;

    const pwEl = document.getElementById('headerPasswordChange');
    if (pwEl) pwEl.onclick = (e) => { e.preventDefault(); window.location.href = "../Auth/change-password.html"; };

    const logoutEl = document.getElementById('headerLogout');
    if (logoutEl) logoutEl.onclick = (e) => {
        e.preventDefault();
        localStorage.clear();
        window.location.href = "../Auth/login.html";
    };
}