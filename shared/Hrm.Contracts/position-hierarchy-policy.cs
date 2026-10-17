namespace Hrm.Contracts;

public static class PositionHierarchyPolicy
{
    /// <summary>
    /// Xác định cấp bậc (Level 1..5) của chức danh.
    /// Ưu tiên cấp bậc Level từ CSDL nếu có giá trị > 0.
    /// Fallback về phân loại từ khóa nếu dữ liệu cũ chưa cấu hình Level.
    /// </summary>
    /// <summary>
    /// Xác định cấp bậc của chức danh.
    /// Ưu tiên số 1: Lấy trực tiếp từ CSDL nếu positionLevel > 0 hoặc tìm thấy trong danh mục position_ranks.
    /// Fallback an toàn: Phân loại từ khóa nếu dữ liệu legacy chưa có cấu hình CSDL.
    /// </summary>
    public static int GetPositionRank(
        string? positionName, 
        string? positionCode = null, 
        int? positionLevel = null,
        IEnumerable<PositionRankDto>? dbRanks = null)
    {
        // 1. Ưu tiên cấp bậc Level từ CSDL nếu có giá trị > 0
        if (positionLevel.HasValue && positionLevel.Value > 0)
        {
            return positionLevel.Value;
        }

        if (string.IsNullOrWhiteSpace(positionName))
        {
            return 1;
        }

        var p = positionName.Trim().ToLowerInvariant();

        // 2. Tra cứu trực tiếp từ danh mục Mức Rank trong CSDL nếu được cung cấp
        if (dbRanks != null)
        {
            var exactRank = dbRanks.FirstOrDefault(r => 
                string.Equals(r.Name, positionName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.Code, positionCode, StringComparison.OrdinalIgnoreCase));
            if (exactRank != null)
            {
                return exactRank.Level;
            }
        }

        // 3. Fallback nhận diện từ khóa (sắp xếp theo thứ tự cụ thể -> chung để tránh match nhầm)
        // Level 11: Chủ tịch / Hội Đồng Quản Trị
        if (p.Contains("chủ tịch") || p.Contains("chu tich") ||
            p.Contains("hội đồng quản trị") || p.Contains("hoi dong quan tri") ||
            p.Contains("chairman") || p.Contains("board"))
        {
            return 11;
        }

        // Level 10: Tổng Giám Đốc / CEO
        if (p.Contains("tổng giám đốc") || p.Contains("tong giam doc") ||
            p.Contains("ceo") || p.Contains("administrator"))
        {
            return 10;
        }

        // Level 8: Phó Giám đốc / Giám đốc khối (kiểm tra TRƯỚC "giám đốc" để không bị match nhầm)
        if (p.Contains("phó giám đốc") || p.Contains("pho giam doc") ||
            p.Contains("deputy director") || p.Contains("head of division") ||
            p.Contains("phó tổng") || p.Contains("pho tong"))
        {
            return 8;
        }

        // Level 9: Giám đốc / Ban Giám Đốc / Director
        if (p.Contains("giám đốc") || p.Contains("giam doc") ||
            p.Contains("director") || p.Contains("ban giám đốc"))
        {
            return 9;
        }

        // Level 6: Phó phòng / Phó ban (kiểm tra TRƯỚC "trưởng phòng" để không bị match nhầm)
        if (p.Contains("phó phòng") || p.Contains("pho phong") ||
            p.Contains("phó trưởng phòng") || p.Contains("pho truong phong") ||
            p.Contains("phó ban") || p.Contains("pho ban") ||
            p.Contains("deputy manager"))
        {
            return 6;
        }

        // Level 7: Trưởng phòng / Trưởng ban / Quản lý đơn vị
        if (p.Contains("trưởng phòng") || p.Contains("truong phong") ||
            p.Contains("trưởng ban") || p.Contains("truong ban") ||
            p.Contains("phụ trách phòng") || p.Contains("head") ||
            p.Contains("lead manager") || p.Contains("quản trị viên"))
        {
            return 7;
        }

        // Level 5: Trưởng nhóm / Team Leader / Supervisor
        if (p.Contains("trưởng nhóm") || p.Contains("truong nhom") ||
            p.Contains("team leader") || p.Contains("leader") ||
            p.Contains("supervisor"))
        {
            return 5;
        }

        // Level 4: Chuyên viên cao cấp / Senior
        if (p.Contains("chuyên viên chính") || p.Contains("chuyên viên cao cấp") ||
            p.Contains("senior") || p.Contains("lead"))
        {
            return 4;
        }

        // Level 3: Chuyên viên / Kỹ sư chính / Specialist
        if (p.Contains("chuyên viên") || p.Contains("chuyen vien") ||
            p.Contains("specialist") || p.Contains("kỹ sư") || p.Contains("ky su"))
        {
            return 3;
        }

        // Level 2: Nhân viên chính thức (Staff)
        if (p.Contains("nhân viên") || p.Contains("nhan vien") || p.Contains("staff"))
        {
            return 2;
        }

        // Level 1: Thực tập sinh / Phổ thông
        return 1;
    }

    /// <summary>
    /// Lấy tên hiển thị cấp bậc: Ưu tiên lấy từ danh mục Mức Rank trong Database.
    /// </summary>
    public static string GetRankDisplayName(int rank, IEnumerable<PositionRankDto>? dbRanks = null)
    {
        if (dbRanks != null)
        {
            var match = dbRanks.FirstOrDefault(r => r.Level == rank);
            if (match != null)
            {
                return $"Cấp {rank}: {match.Name}";
            }
        }

        return rank switch
        {
            >= 11 => $"Cấp {rank}: Hội Đồng Quản Trị / Lãnh Đạo Cấp Cao",
            10 => "Cấp 10: Tổng Giám Đốc / CEO",
            9 => "Cấp 9: Giám Đốc Điều Hành (Director)",
            8 => "Cấp 8: Phó Giám Đốc / Giám Đốc Khối",
            7 => "Cấp 7: Trưởng Phòng / Trưởng Ban (Manager)",
            6 => "Cấp 6: Phó Trưởng Phòng / Phó Ban",
            5 => "Cấp 5: Trưởng Nhóm / Giám Sát (Team Leader)",
            4 => "Cấp 4: Chuyên Viên Cao Cấp / Senior",
            3 => "Cấp 3: Chuyên Viên / Kỹ Sư",
            2 => "Cấp 2: Nhân Viên Chính Thức (Staff)",
            1 => "Cấp 1: Nhân Viên / Thực Tập Sinh",
            _ => $"Cấp {rank}"
        };
    }

    /// <summary>
    /// Gợi ý Role hệ thống: Ưu tiên lấy trực tiếp DefaultRoleName từ CSDL (bảng position_ranks).
    /// </summary>
    public static string SuggestRoleForPosition(
        string? positionName, 
        string? deptCode = null, 
        int? rank = null, 
        string? defaultRoleFromDb = null,
        IEnumerable<PositionRankDto>? dbRanks = null)
    {
        // 1. Ưu tiên cao nhất: Lấy trực tiếp Role được cấu hình trong CSDL
        if (!string.IsNullOrWhiteSpace(defaultRoleFromDb))
        {
            return defaultRoleFromDb.Trim().ToUpperInvariant();
        }

        var r = rank ?? GetPositionRank(positionName, positionLevel: rank, dbRanks: dbRanks);

        // 2. Tra cứu từ danh mục Mức Rank trong CSDL nếu có
        if (dbRanks != null)
        {
            var rankObj = dbRanks.FirstOrDefault(x => x.Level == r);
            if (!string.IsNullOrWhiteSpace(rankObj?.DefaultRoleName))
            {
                return rankObj.DefaultRoleName.Trim().ToUpperInvariant();
            }
        }

        // 3. Fallback khi chưa cấu hình CSDL
        if (r >= 6) // Trưởng phòng hoặc cao hơn
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
