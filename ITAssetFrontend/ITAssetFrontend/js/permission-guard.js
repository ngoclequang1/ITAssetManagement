/* =========================================================================
   permission-guard.js  –  Central permission management
   
   Role mapping (DB_permission.sql):
     roleId 1 = ADMIN          → canView + canRequest + canApprove + canAdmin
     roleId 2 = General Staff  → canView only
     roleId 3 = Dept Rep       → canView + canRequest
     roleId 4 = Administrator  → canView + canRequest + canApprove
     roleId 5 = Manager        → canView + canRequest
   
   USER_PERMISSION table columns:
     can_view    – access list & detail screens
     can_request – create add/edit/delete/license requests
     can_approve – approve or reject requests
     can_admin   – access User, Department, Master maintenance screens
   
   Usage in HTML (data-perm attribute):
     data-perm="can_request"  → visible only if canRequest = true
     data-perm="can_approve"  → visible only if canApprove = true
     data-perm="can_admin"    → visible only if canAdmin = true
     data-perm="no_request"   → visible only if canRequest = false
========================================================================= */

// ─────────────────────────────────────────────────────────────
// 1. Global permission state (defaults view-only until loaded)
// ─────────────────────────────────────────────────────────────
window.currentPermission = {
    userId:     0,
    canView:    true,
    canRequest: false,
    canApprove: false,
    canAdmin:   false
};

// ─────────────────────────────────────────────────────────────
// 2. Load permission from API; cache in sessionStorage
// ─────────────────────────────────────────────────────────────
async function loadCurrentUserPermission() {
    const userId = parseInt(localStorage.getItem("userId")) || 0;
    if (!userId) return;

    // FIX: kiểm tra cache nhưng chỉ dùng khi userId và roleId khớp
    // tránh trường hợp cache cũ của user khác hoặc role chưa được lưu đúng
    const roleId = parseInt(localStorage.getItem("role")) || 2;
    const cached = sessionStorage.getItem("userPermission");
    if (cached) {
        try {
            const parsed = JSON.parse(cached);
            // Invalidate cache nếu userId hoặc roleId không khớp
            if (parsed.userId === userId && parsed._roleId === roleId) {
                window.currentPermission = parsed;
                applyPermissionToUI();
                return;
            }
        } catch (_) { /* ignore corrupt cache */ }
        // Cache không hợp lệ → xóa đi
        sessionStorage.removeItem("userPermission");
    }

    try {
        const res = await fetchAPI(`/asset-request/permission/${userId}`);
        window.currentPermission = {
            userId:     userId,
            _roleId:    roleId,  // lưu để validate cache sau
            canView:    !!res.canView,
            canRequest: !!res.canRequest,
            canApprove: !!res.canApprove,
            canAdmin:   !!res.canAdmin
        };
    } catch (e) {
        // FIX: fallback dùng roleId từ localStorage (đã được lưu khi login)
        console.warn("[PermGuard] API failed, falling back to roleId:", roleId, e.message);
        window.currentPermission = {
            userId:     userId,
            _roleId:    roleId,
            canView:    true,
            canRequest: [1, 3, 4, 5].includes(roleId),
            canApprove: [1, 4].includes(roleId),
            canAdmin:   roleId === 1
        };
    }

    sessionStorage.setItem("userPermission", JSON.stringify(window.currentPermission));
    applyPermissionToUI();
}

// Call this after admin updates a user's permission
window.clearPermissionCache = function() {
    sessionStorage.removeItem("userPermission");
};

// ─────────────────────────────────────────────────────────────
// 3. Apply permission: show/hide DOM elements by data-perm
// ─────────────────────────────────────────────────────────────
function applyPermissionToUI() {
    const perm = window.currentPermission;

    document.querySelectorAll("[data-perm]").forEach(function(el) {
        const required = el.getAttribute("data-perm");
        let visible = false;
        switch (required) {
            case "can_view":    visible = perm.canView;    break;
            case "can_request": visible = perm.canRequest; break;
            case "can_approve": visible = perm.canApprove; break;
            case "can_admin":   visible = perm.canAdmin;   break;
            case "no_request":  visible = !perm.canRequest; break;
            default:            visible = true;
        }
        el.style.display = visible ? "" : "none";
    });

    // Sidebar: hide admin-only nav section from non-admins
    const adminOnlyNav = document.querySelector(".admin-only");
    if (adminOnlyNav) {
        adminOnlyNav.style.display = perm.canAdmin ? "" : "none";
    }

    // Sidebar: hide Pending Approvals link from non-approvers
    document.querySelectorAll('[data-page="approvals"]').forEach(function(el) {
        el.style.display = perm.canApprove ? "" : "none";
    });

    // Render role info banner
    _renderPermissionBanner(perm);

    // Notify page-level modules (each JS file can define this)
    if (typeof window.onPermissionLoaded === "function") {
        window.onPermissionLoaded(perm);
    }
}

// ─────────────────────────────────────────────────────────────
// 4. Permission banner displayed at top of management pages
// ─────────────────────────────────────────────────────────────
function _renderPermissionBanner(perm) {
    const banner = document.getElementById("permissionBanner");
    if (!banner) return;

    var bg, color, border, text;
    if (perm.canAdmin) {
        bg = "#e8f5e9"; color = "#1b5e20"; border = "1px solid #a5d6a7";
        text = "\u2713 You have <b>ADMIN</b> role \u2013 full system access.";
    } else if (perm.canApprove) {
        bg = "#fff3e0"; color = "#e65100"; border = "1px solid #ffcc02";
        text = "\u2713 You have <b>Administrator</b> role \u2013 you can approve / reject pending requests.";
    } else if (perm.canRequest) {
        bg = "#e3f2fd"; color = "#0d47a1"; border = "1px solid #90caf9";
        text = "\u2713 You have <b>Manager / Rep</b> role \u2013 you can submit add / edit / delete requests.";
    } else {
        bg = "#f5f5f5"; color = "#757575"; border = "1px solid #e0e0e0";
        text = "\u24d8 <b>View-only</b> access. Contact your Manager to request changes.";
    }

    banner.style.display      = "block";
    banner.style.background   = bg;
    banner.style.color        = color;
    banner.style.border       = border;
    banner.style.padding      = "8px 15px";
    banner.style.marginBottom = "10px";
    banner.style.fontSize     = "12px";
    banner.style.fontWeight   = "bold";
    banner.style.borderRadius = "3px";
    banner.innerHTML          = text;
}

// ─────────────────────────────────────────────────────────────
// 5. Guard helpers – wrap callbacks that require a permission
// ─────────────────────────────────────────────────────────────
window.guardRequest = function(callback) {
    if (!window.currentPermission.canRequest) {
        _showPermDenied("You do not have permission to create requests.\nContact your Manager.");
        return;
    }
    callback();
};

window.guardApprove = function(callback) {
    if (!window.currentPermission.canApprove) {
        _showPermDenied("You do not have permission to approve requests.");
        return;
    }
    callback();
};

window.guardAdmin = function(callback) {
    if (!window.currentPermission.canAdmin) {
        _showPermDenied("This action requires ADMIN access.");
        return;
    }
    callback();
};

function _showPermDenied(message) {
    if (typeof showCustomAlert === "function") {
        showCustomAlert(message, "Permission Denied", true);
    } else if (typeof showLicAlert === "function") {
        showLicAlert(message, "Permission Denied", true);
    } else {
        alert("Permission Denied:\n" + message);
    }
}

// ─────────────────────────────────────────────────────────────
// 6. Load approver list into <select> elements
// ─────────────────────────────────────────────────────────────
window.loadApproverOptions = async function(selectIds) {
    try {
        const res = await fetchAPI("/users?Page=1&PageSize=500");
        const users = res.data || res || [];
        var html = '<option value="">-- Select Approver --</option>';
        users.forEach(function(u) {
            var label = (u.username || u.userCode) + (u.email ? " (" + u.email + ")" : "");
            html += '<option value="' + u.userId + '">' + label + '</option>';
        });
        (selectIds || []).forEach(function(id) {
            const el = document.getElementById(id);
            if (el) el.innerHTML = html;
        });
    } catch (e) {
        console.warn("[PermGuard] loadApproverOptions error:", e.message);
    }
};

// ─────────────────────────────────────────────────────────────
// 7. Secondary approver toggle (shared across all modals)
// ─────────────────────────────────────────────────────────────
window.toggleSecondaryApprover = function(flagId, selectId, badgeId) {
    const flag   = document.getElementById(flagId);
    const select = document.getElementById(selectId);
    const badge  = document.getElementById(badgeId);
    if (!flag || !select) return;
    if (flag.checked) {
        select.disabled = false;
        if (badge) badge.style.display = "inline-block";
    } else {
        select.disabled = true;
        select.value    = "";
        if (badge) badge.style.display = "none";
    }
};

// ─────────────────────────────────────────────────────────────
// 8. Dynamic hardware card menu based on permission
// ─────────────────────────────────────────────────────────────
window.buildHardwareMenuHTML = function(assetId) {
    const perm = window.currentPermission;
    const viewItems = [
        '<a onclick="viewHwDetail(' + assetId + ')">&#9658; View Details</a>',
        '<a onclick="handleMenuAction(\'appHistory\', ' + assetId + ')">&#9658; Application History</a>',
        '<a onclick="handleMenuAction(\'invHistory\', ' + assetId + ')">&#9658; Inventory History</a>'
    ].join("");

    if (!perm.canRequest) return viewItems;

    return viewItems + [
        '<hr style="margin:4px 0;border:0;border-top:1px solid #ddd;">',
        '<a onclick="handleMenuAction(\'change\', ' + assetId + ')">&#9658; Change Request</a>',
        '<a onclick="handleMenuAction(\'move\', ' + assetId + ')">&#9658; Move Request</a>',
        '<a onclick="handleMenuAction(\'disposal\', ' + assetId + ')">&#9658; Disposal/Return Request</a>',
        '<a onclick="handleMenuAction(\'failure\', ' + assetId + ')">&#9658; Failure/Collection Request</a>',
        '<hr style="margin:4px 0;border:0;border-top:1px solid #ddd;">',
        '<a onclick="openDeleteHardwareModal(' + assetId + ')" style="color:#d32f2f;">&#9658; Delete Asset</a>'
    ].join("");
};

// ─────────────────────────────────────────────────────────────
// 9. Auto-init
// ─────────────────────────────────────────────────────────────
document.addEventListener("DOMContentLoaded", loadCurrentUserPermission);
window.loadCurrentUserPermission = loadCurrentUserPermission;