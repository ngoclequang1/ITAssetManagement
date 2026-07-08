/* =========================================================================
   LICENSE MANAGEMENT – license-management.js
   Mirrors hardware-management.js / software-management.js patterns
========================================================================= */

/* =========================================================================
   1. BIẾN TOÀN CỤC
========================================================================= */
var licenseData = [];
var licTotal    = 0;
var licPage     = 1;
var licPageSize = 4;

/* =========================================================================
   2. KHỞI TẠO
========================================================================= */

// Local permission cache
var _licPerm = window.currentPermission || {
    canView: true, canRequest: false, canApprove: false, canAdmin: false
};

window.initLicenseManagement = function () {
    // Re-read permission cache
    _licPerm = window.currentPermission || _licPerm;
    _applyLicPermission();
    loadLicenseData();
    setupLicDropdownBehavior();
    loadLicDepartmentOptions();
    loadLicUserOptions();
};

// Called by permission-guard.js after it finishes loading
window.onPermissionLoaded = function(perm) {
    _licPerm = perm;
    _applyLicPermission();
};

function _applyLicPermission() {
    // "+ New Application" button
    const newBtns = document.querySelectorAll("[onclick='openNewApplicationModal()']");
    newBtns.forEach(function(el) {
        el.style.display = _licPerm.canRequest ? "" : "none";
    });
    // data-perm elements
    document.querySelectorAll("[data-perm='can_request']").forEach(function(el) {
        el.style.display = _licPerm.canRequest ? "" : "none";
    });
    // Permission banner
    if (typeof window.applyPermissionToUI === "function") {
        window.applyPermissionToUI();
    }
}

/* =========================================================================
   3. TẢI DỮ LIỆU (SEARCH + LIST)
========================================================================= */
async function loadLicenseData() {
    const listEl = document.getElementById("licenseList");
    if (!listEl) return;
    listEl.innerHTML = "<div style='padding: 20px; text-align:center;'>Loading license data...</div>";

    const mgmtNo      = document.getElementById("filterLicMgmtNo")?.value.trim()    || "";
    const installName = document.getElementById("filterInstallName")?.value.trim()   || "";
    const publisher   = document.getElementById("filterPublisher")?.value.trim()     || "";
    const swType      = document.getElementById("filterSoftwareType")?.value         || "";
    const licType     = document.getElementById("filterLicenseType")?.value          || "";
    const countMeth   = document.getElementById("filterCountingMethod")?.value       || "";
    const academic    = document.getElementById("filterAcademic")?.checked ? true    : null;

    const statusChecks = Array.from(document.querySelectorAll(".filter-status-cb:checked"))
        .map(cb => cb.value);
    const licStatus = statusChecks.length === 1 ? statusChecks[0] : "";

    const payload = {
        LicenseManagementNumber: mgmtNo,
        InstallationName:        installName,
        PublisherName:           publisher,
        SoftwareType:            swType,
        LicenseType:             licType,
        CountingMethod:          countMeth,
        LicenseStatus:           licStatus,
        AcademicFlag:            academic,
        Page:                    licPage,
        PageSize:                licPageSize
    };

    try {
        const res = await fetchAPI("/license/search", {
            method: "POST",
            body: JSON.stringify(payload)
        });

        licenseData = res.data  || [];
        licTotal    = res.total || 0;

        renderLicenseList();
        renderLicPagination();
    } catch (err) {
        console.error("License load error:", err);
        listEl.innerHTML = `<div style="color:red; padding:20px;">Failed to load license data: ${err.message}</div>`;
    }
}

window.searchLicense = function () {
    licPage = 1;
    loadLicenseData();
};

window.clearLicenseSearch = function () {
    ["filterLicMgmtNo","filterInstallName","filterPublisher"].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = "";
    });
    ["filterSoftwareType","filterLicenseType","filterCountingMethod"].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = "";
    });
    if (document.getElementById("filterAcademic")) document.getElementById("filterAcademic").checked = false;
    document.querySelectorAll(".filter-status-cb").forEach(cb => cb.checked = cb.value === "Active");
};

window.resetLicenseSearch = function () {
    clearLicenseSearch();
    searchLicense();
};

/* =========================================================================
   4. RENDER DANH SÁCH CARD
========================================================================= */
function renderLicenseList() {
    const list = document.getElementById("licenseList");
    if (!list) return;

    if (!licenseData || licenseData.length === 0) {
        list.innerHTML = "<div style='padding:20px; text-align:center;'>No licenses found.</div>";
        return;
    }

    list.innerHTML = licenseData.map(lic => {
        // Tính % sử dụng
        const used       = lic.numberOfLicenses - lic.numberAvailable;
        const pct        = lic.numberOfLicenses > 0 ? Math.round((used / lic.numberOfLicenses) * 100) : 0;
        const barClass   = pct >= 90 ? "high" : pct >= 70 ? "medium" : "";
        const isLowStock = lic.numberAvailable === 0;

        // Status tags
        let tags = "";
        if (lic.hasInventory)          tags += `<span class="status-tag tag-inventory">[Inventory Complete]</span>`;
        if (lic.isLinked)              tags += `<span class="status-tag tag-linked">[LINK]</span>`;
        if (lic.isUnstocked)           tags += `<span class="status-tag tag-unstocked">[Unstocked]</span>`;
        if (lic.licenseStatus === "Expired")  tags += `<span class="status-tag tag-expired">[Expired]</span>`;
        if (lic.licenseStatus === "Disposed") tags += `<span class="status-tag tag-disposed">[Disposed]</span>`;
        if (!tags)                     tags  = `<span class="status-tag tag-active">[Active]</span>`;

        // Academic badge
        const academicBadge = lic.academicFlag
            ? `<span style="background:#4a148c; color:white; font-size:10px; padding:1px 6px; border-radius:3px; margin-left:5px;">Academic</span>`
            : "";

        return `
        <div class="license-card">
            <div class="card-header">
                <div class="card-header-left">
                    <input type="checkbox" class="lic-checkbox" value="${lic.licenseId}">
                    <a onclick="viewLicDetail(${lic.licenseId})">
                        ${lic.licenseManagementNumber || "Pending..."} &nbsp;|&nbsp; ${lic.installationName || "-"}
                    </a>
                    ${academicBadge}
                    &nbsp;&nbsp;
                    ${tags}
                </div>
                <div class="card-header-right">
                    <div class="dropdown">
                        <button class="btn-menu" onclick="toggleLicDropdown(event, ${lic.licenseId})">Menu</button>
                        <div id="lic-dropdown-${lic.licenseId}" class="dropdown-content">
                            <a onclick="handleLicMenuAction('change', ${lic.licenseId})">▶ Change Application</a>
                            <a onclick="handleLicMenuAction('move', ${lic.licenseId})">▶ Move Application</a>
                            <a onclick="handleLicMenuAction('split', ${lic.licenseId})">▶ Split Application</a>
                            <a onclick="handleLicMenuAction('disposal', ${lic.licenseId})">▶ Disposal Application</a>
                            <hr style="margin:4px 0; border:0; border-top:1px solid #ddd;">
                            <a onclick="handleLicMenuAction('appHistory', ${lic.licenseId})">▶ Application History</a>
                            <a onclick="handleLicMenuAction('invHistory', ${lic.licenseId})">▶ Inventory History</a>
                            <a onclick="viewLicDetail(${lic.licenseId})">▶ View Details</a>
                        </div>
                    </div>
                </div>
            </div>

            <div class="card-body">
                <!-- Row 1: Basic Info -->
                <div class="cell cell-label">License Management No.</div>
                <div class="cell cell-value">${lic.licenseManagementNumber || "Pending Approval"}</div>
                <div class="cell cell-label">Publisher Name</div>
                <div class="cell cell-value">${lic.publisherName || "-"}</div>

                <!-- Row 2: Type -->
                <div class="cell cell-label">Software Type</div>
                <div class="cell cell-value">${lic.softwareType || "-"}</div>
                <div class="cell cell-label">License Type</div>
                <div class="cell cell-value">${lic.licenseType || "-"}</div>

                <!-- Row 3: Counting -->
                <div class="cell cell-label">Counting Method</div>
                <div class="cell cell-value">${lic.countingMethod || "-"}</div>
                <div class="cell cell-label">License Format</div>
                <div class="cell cell-value">${lic.licenseFormat || "-"}</div>

                <!-- Row 4: Quantity -->
                <div class="cell cell-label">Number of Licenses</div>
                <div class="cell cell-value">
                    <div>
                        <span style="font-weight:bold;">${lic.numberOfLicenses}</span>
                        <span style="color:#555; font-size:11px; margin-left:5px;">(Available: <b class="${isLowStock ? 'low-stock' : ''}">${lic.numberAvailable}</b>)</span>
                    </div>
                    <div class="license-bar-container" style="margin-top: 5px; width: 180px;">
                        <div class="license-bar">
                            <div class="license-bar-fill ${barClass}" style="width: ${pct}%;"></div>
                        </div>
                        <div class="license-bar-text">${used} / ${lic.numberOfLicenses} in use (${pct}%)</div>
                    </div>
                </div>
                <div class="cell cell-label">Management Department</div>
                <div class="cell cell-value">${lic.managementDepartmentName || "-"}</div>

                <!-- Row 5: Dates & Manager -->
                <div class="cell cell-label">Expiry Date</div>
                <div class="cell cell-value">${lic.expiryDate ? new Date(lic.expiryDate).toLocaleDateString() : "-"}</div>
                <div class="cell cell-label">Manager</div>
                <div class="cell cell-value">${lic.managerUsername || "-"}</div>
            </div>
        </div>`;
    }).join("");
}

/* =========================================================================
   5. PAGINATION
========================================================================= */
function renderLicPagination() {
    const totalPages = Math.ceil(licTotal / licPageSize) || 1;
    let btns = `<button onclick="changeLicPage(1)"><<</button>
                <button onclick="changeLicPage(${licPage > 1 ? licPage - 1 : 1})"><</button>`;

    for (let i = 1; i <= totalPages; i++) {
        btns += `<button class="${i === licPage ? 'active' : ''}" onclick="changeLicPage(${i})">${i}</button>`;
    }

    btns += `<button onclick="changeLicPage(${licPage < totalPages ? licPage + 1 : totalPages})">></button>
             <button onclick="changeLicPage(${totalPages})">>></button>`;

    const start = licTotal === 0 ? 0 : (licPage - 1) * licPageSize + 1;
    const end   = Math.min(licPage * licPageSize, licTotal);

    ["licPaginationTop","licPaginationBottom"].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.innerHTML = btns;
    });
    ["licTotalTop","licTotalBottom"].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.innerText = licTotal;
    });
    ["licShowStartTop","licShowStartBottom"].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.innerText = start;
    });
    ["licShowEndTop","licShowEndBottom"].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.innerText = end;
    });
}

window.changeLicPage = function (page) {
    licPage = page;
    loadLicenseData();
};

window.changeLicPageSize = function (size) {
    licPageSize = parseInt(size);
    licPage = 1;
    ["licPageSizeTop","licPageSizeBottom"].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = size;
    });
    loadLicenseData();
};

/* =========================================================================
   6. DROPDOWN
========================================================================= */
window.toggleLicDropdown = function (event, id) {
    event.stopPropagation();
    const curr = document.getElementById(`lic-dropdown-${id}`);
    document.querySelectorAll('.dropdown-content').forEach(el => {
        if (el !== curr) el.classList.remove('show');
    });
    if (curr) curr.classList.toggle("show");
};

window.toggleBulkDropdown = function (event, btnEl) {
    event.stopPropagation();
    const drop = btnEl.nextElementSibling;
    document.querySelectorAll('.dropdown-content').forEach(el => {
        if (el !== drop) el.classList.remove('show');
    });
    if (drop) drop.classList.toggle("show");
};

function setupLicDropdownBehavior() {
    window.addEventListener("click", function (e) {
        if (!e.target.matches('.btn-menu') && !e.target.matches('.btn-outline')) {
            document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
        }
    });
}

/* =========================================================================
   7. MENU ACTIONS
========================================================================= */
window.handleLicMenuAction = async function (action, licId) {
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
    const lic = licenseData.find(l => l.licenseId === licId);

    // Actions requiring canRequest
    const writeActions = ['change', 'move', 'split', 'disposal'];
    if (writeActions.includes(action) && !_licPerm.canRequest) {
        showLicAlert(
            "You do not have permission to create requests.\nContact your Manager.",
            "Permission Denied", true
        );
        return;
    }

    switch (action) {
        case 'change':
            openChangeModal(lic);
            break;
        case 'move':
            openMoveModal(lic);
            break;
        case 'split':
            openSplitModal(lic);
            break;
        case 'disposal':
            openDisposalModal(lic);
            break;
        case 'appHistory':
            loadLicAppHistory(licId);
            break;
        case 'invHistory':
            loadLicInvHistory(licId);
            break;
    }
};

/* =========================================================================
   8. VIEW DETAIL (READ-ONLY)
========================================================================= */
window.viewLicDetail = async function (id) {
    try {
        const lic = await fetchAPI(`/license/${id}`);
        const tbody = document.getElementById("licDetailTable");
        if (!tbody) return;

        const rows = [
            ["License ID",              lic.licenseId],
            ["License Management No.",  lic.licenseManagementNumber || "Pending Approval"],
            ["License Key",             lic.licenseKey || "-"],
            ["Installation Name",       lic.installationName || "-"],
            ["Publisher Name",          lic.publisherName || "-"],
            ["Software Type",           lic.softwareType || "-"],
            ["License Type",            lic.licenseType || "-"],
            ["License Format",          lic.licenseFormat || "-"],
            ["Counting Method",         lic.countingMethod || "-"],
            ["Academic Flag",           lic.academicFlag ? "Yes (Academic)" : "No"],
            ["Number of Licenses",      lic.numberOfLicenses],
            ["Number Available",        lic.numberAvailable],
            ["License Status",          lic.licenseStatus || "-"],
            ["Expiry Date",             lic.expiryDate ? new Date(lic.expiryDate).toLocaleDateString() : "-"],
            ["Disposal Date",           lic.disposalDate ? new Date(lic.disposalDate).toLocaleDateString() : "-"],
            ["Management Department",   lic.managementDepartmentName || "-"],
            ["Manager",                 lic.managerUsername || "-"],
            ["Parent License No.",      lic.parentLicenseManagementNumber || "-"],
            ["Description",             lic.description || "-"],
            ["Created At",              lic.createdAt ? new Date(lic.createdAt).toLocaleDateString() : "-"],
            ["Updated At",              lic.updatedAt ? new Date(lic.updatedAt).toLocaleDateString() : "-"],
        ];

        tbody.innerHTML = rows.map(([label, value]) => `
            <tr>
                <th>${label}</th>
                <td>${value}</td>
            </tr>`).join("");

        document.getElementById("licDetailModal").style.display = "block";
    } catch (e) {
        showLicAlert("Error loading license detail: " + e.message, "Error", true);
    }
};

/* =========================================================================
   9. OPEN MODALS CHO TỪNG LOẠI APPLICATION
========================================================================= */
function openChangeModal(lic) {
    if (!lic) return;
    document.getElementById("changeLicId").value        = lic.licenseId;
    document.getElementById("changeLicMgmtNo").innerText    = lic.licenseManagementNumber || "Pending";
    document.getElementById("changeLicInstallName").innerText = lic.installationName || "";

    // Pre-fill current values
    document.getElementById("chgPublisher").value       = lic.publisherName || "";
    document.getElementById("chgSoftwareType").value    = lic.softwareType  || "";
    document.getElementById("chgLicenseType").value     = lic.licenseType   || "";
    document.getElementById("chgLicenseFormat").value   = lic.licenseFormat || "";
    document.getElementById("chgCountingMethod").value  = lic.countingMethod|| "";
    document.getElementById("chgNumLicenses").value     = "";
    document.getElementById("chgExpiryDate").value      = lic.expiryDate ? lic.expiryDate.split("T")[0] : "";
    document.getElementById("chgDescription").value     = lic.description  || "";
    document.getElementById("chgAcademicFlag").value    = "";

    document.getElementById("licChangeModal").style.display = "block";
}

function openMoveModal(lic) {
    if (!lic) return;
    document.getElementById("moveLicId").value               = lic.licenseId;
    document.getElementById("moveLicMgmtNo").innerText       = lic.licenseManagementNumber || "Pending";
    document.getElementById("moveLicInstallName").innerText  = lic.installationName || "-";
    document.getElementById("moveLicCurrentDept").innerText  = lic.managementDepartmentName || "None";
    document.getElementById("moveLicCurrentMgr").innerText   = lic.managerUsername || "-";
    document.getElementById("moveDeptId").value              = "";
    document.getElementById("moveManagerUserId").value       = "";
    document.getElementById("licMoveModal").style.display    = "block";
}

function openSplitModal(lic) {
    if (!lic) return;
    document.getElementById("splitLicId").value              = lic.licenseId;
    document.getElementById("splitLicAvailable").value       = lic.numberAvailable;
    document.getElementById("splitLicMgmtNo").innerText      = lic.licenseManagementNumber || "Pending";
    document.getElementById("splitLicInstallName").innerText = lic.installationName || "-";
    document.getElementById("splitLicTotal").innerText       = lic.numberOfLicenses;
    document.getElementById("splitLicAvailDisplay").innerText= lic.numberAvailable;
    document.getElementById("splitCount").value = "";
    document.getElementById("splitPreview").innerText = "";
    document.getElementById("licSplitModal").style.display = "block";
}

window.updateSplitPreview = function () {
    const available = parseInt(document.getElementById("splitLicAvailable").value) || 0;
    const splitCount = parseInt(document.getElementById("splitCount").value) || 0;
    const prev = document.getElementById("splitPreview");

    if (splitCount <= 0) {
        prev.innerText = "";
        prev.style.color = "#555";
        return;
    }
    if (splitCount > available) {
        prev.innerText = `⚠ Split count (${splitCount}) exceeds available (${available})`;
        prev.style.color = "#d32f2f";
    } else {
        const remaining = available - splitCount;
        prev.innerText = `After split: Source will have ${remaining} available | Child license: ${splitCount}`;
        prev.style.color = "#1b5e20";
    }
};

function openDisposalModal(lic) {
    if (!lic) return;
    document.getElementById("disposalLicId").value               = lic.licenseId;
    document.getElementById("disposalLicMgmtNo").innerText       = lic.licenseManagementNumber || "Pending";
    document.getElementById("disposalLicInstallName").innerText  = lic.installationName || "-";
    document.getElementById("disposalDate").value                = "";
    document.getElementById("disposalRemarks").value             = "";

    const errEl  = document.getElementById("disposalLinkedError");
    const btnEl  = document.getElementById("btnDisposalConfirm");

    if (lic.isLinked) {
        errEl.style.display  = "block";
        btnEl.disabled       = true;
        btnEl.style.opacity  = "0.5";
        btnEl.style.cursor   = "not-allowed";
    } else {
        errEl.style.display  = "none";
        btnEl.disabled       = false;
        btnEl.style.opacity  = "1";
        btnEl.style.cursor   = "pointer";
    }

    document.getElementById("licDisposalModal").style.display = "block";
}

/* =========================================================================
   10. SUBMIT APPLICATIONS → GỌI API
========================================================================= */
window.submitNewApplication = async function () {
    const installName   = document.getElementById("newInstallName").value.trim();
    const softwareType  = document.getElementById("newSoftwareType").value;
    const licenseType   = document.getElementById("newLicenseType").value;
    const licenseFormat = document.getElementById("newLicenseFormat").value;
    const countingMethod= document.getElementById("newCountingMethod").value;
    const numLicenses   = parseInt(document.getElementById("newNumLicenses").value) || 0;
    const firstApp      = document.getElementById("firstAppNew").value;
    const secAppOn      = document.getElementById("secAppFlagNew").checked;
    const secApp        = document.getElementById("secAppNew").value;

    if (!installName || !softwareType || !licenseType || !licenseFormat || !countingMethod || numLicenses <= 0) {
        showLicAlert("Please fill in all required fields (Must).", "Validation Error", true);
        return;
    }
    if (!firstApp) {
        showLicAlert("Please select a First Approver.", "Validation Error", true);
        return;
    }
    if (secAppOn && !secApp) {
        showLicAlert("Please select a Secondary Approver.", "Validation Error", true);
        return;
    }

    const ok = await showLicConfirm("Submit this New License Application? The license management number will be generated after approval.", "Confirm Submission");
    if (!ok) return;

    const payload = {
        InstallationName:      installName,
        PublisherName:         document.getElementById("newPublisher").value.trim(),
        SoftwareType:          softwareType,
        LicenseType:           licenseType,
        LicenseFormat:         licenseFormat,
        CountingMethod:        countingMethod,
        AcademicFlag:          document.getElementById("newAcademicFlag").checked,
        LicenseKey:            document.getElementById("newLicenseKey").value.trim(),
        NumberOfLicenses:      numLicenses,
        ExpiryDate:            document.getElementById("newExpiryDate").value || null,
        Description:           document.getElementById("newDescription").value.trim(),
        ManagementDepartmentId: document.getElementById("newMgmtDept").value
            ? parseInt(document.getElementById("newMgmtDept").value) : null,
        FirstApproverId:       parseInt(firstApp),
        SecondApproverId:      secAppOn && secApp ? parseInt(secApp) : null
    };

    try {
        await fetchAPI("/license/new-application", { method: "POST", body: JSON.stringify(payload) });
        showLicAlert("New License Application submitted successfully! Pending approval.", "Success");
        closeLicModal("licNewModal");
        loadLicenseData();
    } catch (e) {
        showLicAlert("Error: " + e.message, "Error", true);
    }
};

window.submitChangeApplication = async function () {
    const licId    = document.getElementById("changeLicId").value;
    const firstApp = document.getElementById("firstAppChange").value;
    const secAppOn = document.getElementById("secAppFlagChange").checked;
    const secApp   = document.getElementById("secAppChange").value;

    if (!firstApp) {
        showLicAlert("Please select a First Approver.", "Validation Error", true);
        return;
    }
    if (secAppOn && !secApp) {
        showLicAlert("Please select a Secondary Approver.", "Validation Error", true);
        return;
    }

    const ok = await showLicConfirm("Submit this Change Application?", "Confirm");
    if (!ok) return;

    const payload = {
        PublisherName:    document.getElementById("chgPublisher").value.trim() || null,
        SoftwareType:     document.getElementById("chgSoftwareType").value     || null,
        LicenseType:      document.getElementById("chgLicenseType").value      || null,
        LicenseFormat:    document.getElementById("chgLicenseFormat").value    || null,
        CountingMethod:   document.getElementById("chgCountingMethod").value   || null,
        NumberOfLicenses: document.getElementById("chgNumLicenses").value
            ? parseInt(document.getElementById("chgNumLicenses").value) : null,
        ExpiryDate:       document.getElementById("chgExpiryDate").value || null,
        Description:      document.getElementById("chgDescription").value.trim() || null,
        ManagementDepartmentId: document.getElementById("chgMgmtDept").value
            ? parseInt(document.getElementById("chgMgmtDept").value) : null,
        AcademicFlag:     document.getElementById("chgAcademicFlag").value !== ""
            ? document.getElementById("chgAcademicFlag").value === "true" : null,
        FirstApproverId:  parseInt(firstApp),
        SecondApproverId: secAppOn && secApp ? parseInt(secApp) : null
    };

    try {
        await fetchAPI(`/license/${licId}/change-application`, { method: "POST", body: JSON.stringify(payload) });
        showLicAlert("Change Application submitted! Pending approval.", "Success");
        closeLicModal("licChangeModal");
        loadLicenseData();
    } catch (e) {
        showLicAlert("Error: " + e.message, "Error", true);
    }
};

window.submitMoveApplication = async function () {
    const licId       = document.getElementById("moveLicId").value;
    const deptId      = document.getElementById("moveDeptId").value;
    const managerId   = document.getElementById("moveManagerUserId").value;
    const firstApp    = document.getElementById("firstAppMove").value;
    const secAppOn    = document.getElementById("secAppFlagMove").checked;
    const secApp      = document.getElementById("secAppMove").value;

    if (!deptId) {
        showLicAlert("Please select a destination department.", "Validation Error", true);
        return;
    }
    // Validate Person in charge – bắt buộc theo SKILL_LICENSE_MANAGEMENT §3.3
    if (!managerId) {
        showLicAlert("Please select a person in charge of the location.", "Validation Error", true);
        return;
    }
    if (!firstApp) {
        showLicAlert("Please select a First Approver.", "Validation Error", true);
        return;
    }
    if (secAppOn && !secApp) {
        showLicAlert("Please select a Secondary Approver.", "Validation Error", true);
        return;
    }

    const ok = await showLicConfirm("Submit this Move Application?", "Confirm");
    if (!ok) return;

    const payload = {
        DestinationDepartmentId: parseInt(deptId),
        NewManagerUserId:        parseInt(managerId),   // Person in charge of the location
        FirstApproverId:         parseInt(firstApp),
        SecondApproverId:        secAppOn && secApp ? parseInt(secApp) : null
    };

    try {
        await fetchAPI(`/license/${licId}/move-application`, { method: "POST", body: JSON.stringify(payload) });
        showLicAlert("Move Application submitted! Pending approval.", "Success");
        closeLicModal("licMoveModal");
        loadLicenseData();
    } catch (e) {
        showLicAlert("Error: " + e.message, "Error", true);
    }
};

window.submitSplitApplication = async function () {
    const licId     = document.getElementById("splitLicId").value;
    const available = parseInt(document.getElementById("splitLicAvailable").value) || 0;
    const splitCount= parseInt(document.getElementById("splitCount").value) || 0;
    const deptId    = document.getElementById("splitDeptId").value;
    const firstApp  = document.getElementById("firstAppSplit").value;
    const secAppOn  = document.getElementById("secAppFlagSplit").checked;
    const secApp    = document.getElementById("secAppSplit").value;

    if (splitCount <= 0) {
        showLicAlert("Split count must be greater than 0.", "Validation Error", true);
        return;
    }
    if (splitCount > available) {
        showLicAlert(`Split count (${splitCount}) exceeds available licenses (${available}).`, "Validation Error", true);
        return;
    }
    if (!deptId) {
        showLicAlert("Please select a destination department.", "Validation Error", true);
        return;
    }
    if (!firstApp) {
        showLicAlert("Please select a First Approver.", "Validation Error", true);
        return;
    }
    if (secAppOn && !secApp) {
        showLicAlert("Please select a Secondary Approver.", "Validation Error", true);
        return;
    }

    const ok = await showLicConfirm(
        `Split ${splitCount} license(s) from this license? A new child license will be created after approval.`,
        "Confirm Split"
    );
    if (!ok) return;

    const payload = {
        SplitCount:             splitCount,
        DestinationDepartmentId: parseInt(deptId),
        FirstApproverId:         parseInt(firstApp),
        SecondApproverId:        secAppOn && secApp ? parseInt(secApp) : null
    };

    try {
        await fetchAPI(`/license/${licId}/split-application`, { method: "POST", body: JSON.stringify(payload) });
        showLicAlert("Split Application submitted! Pending approval.", "Success");
        closeLicModal("licSplitModal");
        loadLicenseData();
    } catch (e) {
        showLicAlert("Error: " + e.message, "Error", true);
    }
};

window.submitDisposalApplication = async function () {
    const licId      = document.getElementById("disposalLicId").value;
    const dispDate   = document.getElementById("disposalDate").value;
    const remarks    = document.getElementById("disposalRemarks").value.trim();
    const firstApp   = document.getElementById("firstAppDisposal").value;
    const secAppOn   = document.getElementById("secAppFlagDisposal").checked;
    const secApp     = document.getElementById("secAppDisposal").value;

    if (!dispDate) {
        showLicAlert("Please select a disposal date.", "Validation Error", true);
        return;
    }
    if (!firstApp) {
        showLicAlert("Please select a First Approver.", "Validation Error", true);
        return;
    }
    if (secAppOn && !secApp) {
        showLicAlert("Please select a Secondary Approver.", "Validation Error", true);
        return;
    }

    const ok = await showLicConfirm(
        "Submit Disposal Application? The license will be marked as Disposed after approval.",
        "Confirm Disposal"
    );
    if (!ok) return;

    const payload = {
        DisposalDate:    dispDate,
        Remarks:         remarks,
        FirstApproverId: parseInt(firstApp),
        SecondApproverId: secAppOn && secApp ? parseInt(secApp) : null
    };

    try {
        await fetchAPI(`/license/${licId}/disposal-application`, { method: "POST", body: JSON.stringify(payload) });
        showLicAlert("Disposal Application submitted! Pending approval.", "Success");
        closeLicModal("licDisposalModal");
        loadLicenseData();
    } catch (e) {
        showLicAlert("Error: " + e.message, "Error", true);
    }
};

/* =========================================================================
   11. HISTORY
========================================================================= */
window.loadLicAppHistory = async function (licId) {
    try {
        const data = await fetchAPI(`/license/${licId}/history`);
        const tbody = document.getElementById("licAppHistoryBody");
        if (!tbody) return;

        if (!data || data.length === 0) {
            tbody.innerHTML = `<tr><td colspan="5" style="text-align:center; padding:12px;">No application history found.</td></tr>`;
        } else {
            tbody.innerHTML = data.map(item => `
                <tr>
                    <td>${item.applicationId}</td>
                    <td>${item.applicant || "-"}</td>
                    <td>${item.applicationDate ? new Date(item.applicationDate).toLocaleDateString() : "-"}</td>
                    <td>${item.applicationType || "-"}</td>
                    <td>${item.description || "-"}</td>
                </tr>`).join("");
        }

        document.getElementById("licAppHistoryModal").style.display = "block";
    } catch (e) {
        showLicAlert("Error loading application history: " + e.message, "Error", true);
    }
};

window.loadLicInvHistory = async function (licId) {
    try {
        const data = await fetchAPI(`/license/${licId}/inventory-history`);
        const tbody = document.getElementById("licInvHistoryBody");
        if (!tbody) return;

        if (!data || data.length === 0) {
            tbody.innerHTML = `<tr><td colspan="5" style="text-align:center; padding:12px;">No inventory history found.</td></tr>`;
        } else {
            tbody.innerHTML = data.map(item => `
                <tr>
                    <td>${item.inventoryDate ? new Date(item.inventoryDate).toLocaleDateString() : "-"}</td>
                    <td>${item.inventoryTaker || "-"}</td>
                    <td>
                        <span style="background: ${item.inventoryStatus === 'Completed' ? '#1b5e20' : '#b71c1c'}; color:white; padding:2px 7px; border-radius:3px; font-size:11px;">
                            ${item.inventoryStatus || "-"}
                        </span>
                    </td>
                    <td>${item.remarks || "-"}</td>
                    <td>
                        ${item.inventoryStatus === 'Completed'
                            ? `<button onclick="returnInventoryToIncomplete(${item.inventoryId})"
                                style="background:#f39c12; color:white; border:none; padding:3px 8px; cursor:pointer; font-size:11px; border-radius:2px;">
                                Return to Incomplete</button>`
                            : "-"}
                    </td>
                </tr>`).join("");
        }

        document.getElementById("licInvHistoryModal").style.display = "block";
    } catch (e) {
        showLicAlert("Error loading inventory history: " + e.message, "Error", true);
    }
};

// Return to incomplete (rollback inventory status)
window.returnInventoryToIncomplete = async function (inventoryId) {
    const ok = await showLicConfirm(
        "Return this inventory record to 'Not Yet' status? This will undo the completed inventory.",
        "Confirm Return"
    );
    if (!ok) return;

    try {
        await fetchAPI(`/license/inventory/${inventoryId}/return-incomplete`, { method: "PUT" });
        showLicAlert("Inventory status returned to incomplete.", "Success");
        // Reload hiện tại: tìm licId từ dữ liệu đang mở
        closeLicModal("licInvHistoryModal");
    } catch (e) {
        showLicAlert("Error: " + e.message, "Error", true);
    }
};

/* =========================================================================
   12. OPEN NEW APPLICATION MODAL
========================================================================= */
window.openNewApplicationModal = function () {
    // Reset toàn bộ form
    ["newInstallName","newPublisher","newLicenseKey","newDescription"].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = "";
    });
    ["newSoftwareType","newLicenseType","newLicenseFormat","newCountingMethod","newMgmtDept",
     "firstAppNew","secAppNew"].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = "";
    });
    document.getElementById("newNumLicenses").value = "";
    document.getElementById("newExpiryDate").value  = "";
    document.getElementById("newAcademicFlag").checked   = false;
    document.getElementById("secAppFlagNew").checked     = true;
    document.getElementById("secAppBadgeNew").style.display = "inline-block";
    document.getElementById("secAppNew").disabled = false;

    document.getElementById("licNewModal").style.display = "block";
};

/* =========================================================================
   13. TOGGLE SECONDARY APPROVER
========================================================================= */
window.toggleSecApp = function (suffix) {
    const flagEl  = document.getElementById(`secAppFlag${suffix}`);
    const selectEl= document.getElementById(`secApp${suffix}`);
    const badgeEl = document.getElementById(`secAppBadge${suffix}`);

    if (!flagEl || !selectEl || !badgeEl) return;

    if (flagEl.checked) {
        selectEl.disabled             = false;
        badgeEl.style.display         = "inline-block";
    } else {
        selectEl.disabled             = true;
        selectEl.value                = "";
        badgeEl.style.display         = "none";
    }
};

/* =========================================================================
   14. LOAD DEPARTMENT / USER OPTIONS VÀO CÁC SELECT
========================================================================= */
window.loadLicDepartmentOptions = async function () {
    try {
        const res   = await fetchAPI("/departments?Page=1&PageSize=500");
        const depts = res.data || [];
        let html    = '<option value="">-- Select Department --</option>';
        depts.forEach(d => {
            html += `<option value="${d.departmentId}">${d.departmentCode} - ${d.departmentName}</option>`;
        });

        ["newMgmtDept","chgMgmtDept","moveDeptId","splitDeptId"].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.innerHTML = html;
        });
    } catch (e) {
        console.error("Load departments error:", e);
    }
};

window.loadLicUserOptions = async function () {
    try {
        const res   = await fetchAPI("/users?Page=1&PageSize=500");
        const users = res.data || res || [];
        let html    = '<option value="">-- Select Approver --</option>';
        users.forEach(u => {
            html += `<option value="${u.userId}">${u.username} (${u.email || u.userCode})</option>`;
        });

        ["firstAppNew","secAppNew",
         "firstAppChange","secAppChange",
         "firstAppMove","secAppMove",
         "moveManagerUserId",                     // Person in charge of the location
         "firstAppSplit","secAppSplit",
         "firstAppDisposal","secAppDisposal"].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.innerHTML = html;
        });
    } catch (e) {
        console.error("Load users error:", e);
    }
};

/* =========================================================================
   15. CHECKBOX & DOWNLOAD
========================================================================= */
window.selectAllLicenses = function (isSelect) {
    document.querySelectorAll('.lic-checkbox').forEach(cb => cb.checked = isSelect);
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
};

window.getSelectedLicenseIds = function () {
    return Array.from(document.querySelectorAll('.lic-checkbox:checked')).map(cb => parseInt(cb.value));
};

window.downloadSelectedLicenses = function () {
    const ids = getSelectedLicenseIds();
    if (ids.length === 0) {
        showLicAlert("Please select at least 1 license to download.", "Notice");
        return;
    }
    exportLicToCSV(licenseData.filter(l => ids.includes(l.licenseId)), 'Selected_Licenses.csv');
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
};

window.downloadAllLicenses = function () {
    if (licenseData.length === 0) {
        showLicAlert("No data to download.", "Notice");
        return;
    }
    exportLicToCSV(licenseData, 'All_Licenses.csv');
    document.querySelectorAll('.dropdown-content').forEach(el => el.classList.remove('show'));
};

function exportLicToCSV(dataArr, filename) {
    if (!dataArr || !dataArr.length) return;
    const headers = Object.keys(dataArr[0]);
    const rows    = [headers.join(',')];
    for (const row of dataArr) {
        rows.push(headers.map(h => `"${String(row[h] ?? '').replace(/"/g,'""')}"`).join(','));
    }
    const blob = new Blob(['\uFEFF' + rows.join('\n')], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href  = URL.createObjectURL(blob);
    link.setAttribute('download', filename);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

/* =========================================================================
   16. MODAL UTILITIES
========================================================================= */
window.closeLicModal = function (modalId) {
    const el = document.getElementById(modalId);
    if (el) el.style.display = "none";
};

window.showLicAlert = function (message, title = "Notification", isError = false) {
    const msgEl    = document.getElementById("licAlertMessage");
    const titleEl  = document.getElementById("licAlertTitle");
    const headerEl = document.getElementById("licAlertHeader");
    const modalEl  = document.getElementById("licAlertModal");

    if (!msgEl) { alert(`${title}: ${message}`); return; }

    msgEl.innerText   = message;
    if (titleEl)  titleEl.innerText   = title;
    if (headerEl) headerEl.style.backgroundColor = isError ? "#d32f2f" : "#000066";
    if (modalEl)  modalEl.style.display = "block";
};

window.showLicConfirm = function (message, title = "Confirm Action") {
    return new Promise(resolve => {
        const msgEl   = document.getElementById("licConfirmMessage");
        const titleEl = document.getElementById("licConfirmTitle");
        const modalEl = document.getElementById("licConfirmModal");

        if (!msgEl) { resolve(confirm(`${title}\n\n${message}`)); return; }

        msgEl.innerText   = message;
        if (titleEl) titleEl.innerText = title;
        if (modalEl) modalEl.style.display = "block";

        const btnYes = document.getElementById("licBtnConfirmYes");
        if (btnYes) {
            const newBtn = btnYes.cloneNode(true);
            btnYes.parentNode.replaceChild(newBtn, btnYes);
            newBtn.addEventListener("click", () => {
                closeLicModal("licConfirmModal");
                resolve(true);
            });
        }

        const handleCancel = () => { closeLicModal("licConfirmModal"); resolve(false); };
        const btnCancel = document.querySelector("#licConfirmModal .btn-cancel");
        const btnClose  = document.querySelector("#licConfirmModal .close");
        if (btnCancel) btnCancel.onclick = handleCancel;
        if (btnClose)  btnClose.onclick  = handleCancel;
    });
};