namespace Hrm.Contracts;

public static class PositionHierarchyPolicy
{
    /// <summary>
    /// Xác định cấp bậc (Level 1..5) của chức danh dựa trên nhóm từ khóa:
    /// Level 5: Giám đốc / Ban Giám Đốc / CEO / Director / Administrator
    /// Level 4: Trưởng phòng / Trưởng ban / Head / Manager / Lead Manager
    /// Level 3: Trưởng nhóm / Phó phòng / Team Leader / Supervisor / Lead
    /// Level 2: Chuyên viên / Kỹ sư chính / Senior / Specialist
    /// Level 1: Nhân viên / Kỹ thuật viên / Thực tập sinh / Staff / Junior
    /// </summary>
    public static int GetPositionRank(string? positionName, string? positionCode = null)
    {
        if (string.IsNullOrWhiteSpace(positionName))
        {
            return 1;
        }

        var p = positionName.Trim().ToLowerInvariant();

        // Level 5: Ban Lãnh đạo / Giám đốc
        if (p.Contains("giám đốc") || p.Contains("giam doc") ||
            p.Contains("director") || p.Contains("ceo") ||
            p.Contains("ban giám đốc") || p.Contains("tổng giám đốc") ||
            p.Contains("administrator"))
        {
            return 5;
        }

        // Level 4: Trưởng phòng / Quản lý đơn vị
        if (p.Contains("trưởng phòng") || p.Contains("truong phong") ||
            p.Contains("trưởng ban") || p.Contains("truong ban") ||
            p.Contains("phụ trách phòng") || p.Contains("head") ||
            p.Contains("lead manager") || p.Contains("quản trị viên"))
        {
            return 4;
        }

        // Level 3: Trưởng nhóm / Phó phòng
        if (p.Contains("phó phòng") || p.Contains("pho phong") ||
            p.Contains("phó trưởng phòng") || p.Contains("trưởng nhóm") ||
            p.Contains("truong nhom") || p.Contains("team leader") ||
            p.Contains("leader") || p.Contains("supervisor") ||
            p.Contains("phó ban"))
        {
            return 3;
        }

        // Level 2: Chuyên viên / Kỹ sư / Senior
        if (p.Contains("chuyên viên") || p.Contains("chuyen vien") ||
            p.Contains("senior") || p.Contains("specialist") ||
            p.Contains("kỹ sư") || p.Contains("ky su"))
        {
            return 2;
        }

        // Level 1: Nhân viên / Phổ thông
        return 1;
    }

    public static string GetRankDisplayName(int rank) => rank switch
    {
        5 => "Cấp 5: Ban Giám Đốc / Giám Đốc",
        4 => "Cấp 4: Trưởng Phòng / Quản Lý",
        3 => "Cấp 3: Trưởng Nhóm / Phó Phòng",
        2 => "Cấp 2: Chuyên Viên",
        _ => "Cấp 1: Nhân Viên"
    };

    /// <summary>
    /// Gợi ý Role hệ thống phù hợp khi được bổ nhiệm vào vị trí mới.
    /// </summary>
    public static string SuggestRoleForPosition(string? positionName, string? deptCode = null)
    {
        var rank = GetPositionRank(positionName);
        if (rank >= 4)
        {
            if (string.Equals(deptCode, "HR", StringComparison.OrdinalIgnoreCase))
            {
                return "HR_MANAGER";
            }

            return "MANAGER";
        }

        return "EMPLOYEE";
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ về thẩm quyền bổ nhiệm/thăng chức:
    /// 1. Vị trí mới phải có cấp bậc cao hơn vị trí hiện tại.
    /// 2. Người thực hiện (nếu không phải Admin/BGD) phải cùng phòng ban.
    /// 3. Người thực hiện phải có cấp bậc cao hơn cả vị trí hiện tại lẫn vị trí mới được thăng chức.
    /// </summary>
    public static bool ValidatePromotion(
        int actorRank,
        bool isActorAdminOrBoard,
        long actorDepartmentId,
        long targetDepartmentId,
        int currentTargetRank,
        int newTargetRank,
        out string? validationError)
    {
        if (newTargetRank <= currentTargetRank)
        {
            validationError = "Vị trí bổ nhiệm mới phải có cấp bậc cao hơn chức danh hiện tại của nhân sự.";
            return false;
        }

        if (isActorAdminOrBoard)
        {
            validationError = null;
            return true;
        }

        if (actorDepartmentId != targetDepartmentId)
        {
            validationError = "Bạn chỉ có thẩm quyền bổ nhiệm/thăng chức cho nhân sự trong phạm vi phòng ban của mình.";
            return false;
        }

        if (actorRank <= currentTargetRank)
        {
            validationError = "Bạn không có thẩm quyền thăng chức cho nhân sự có cấp bậc ngang hàng hoặc cao hơn bạn.";
            return false;
        }

        if (actorRank <= newTargetRank)
        {
            validationError = $"Bạn chỉ có thẩm quyền bổ nhiệm nhân sự lên vị trí có cấp bậc thấp hơn vị trí của bạn (Vị trí mới: {GetRankDisplayName(newTargetRank)}, Vị trí của bạn: {GetRankDisplayName(actorRank)}).";
            return false;
        }

        validationError = null;
        return true;
    }
}
