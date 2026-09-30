namespace Hrm.Contracts;

public static class DepartmentPositionPolicy
{
    /// <summary>
    /// Kiểm tra chức danh/vị trí có tương thích và phù hợp với phòng ban hay không.
    /// </summary>
    public static bool IsCompatible(string? deptCode, string? deptName, string? positionName, string? positionCode = null)
    {
        if (string.IsNullOrWhiteSpace(deptCode) && string.IsNullOrWhiteSpace(deptName))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(positionName))
        {
            return false;
        }

        var dCode = (deptCode ?? string.Empty).Trim().ToUpperInvariant();
        var dName = (deptName ?? string.Empty).Trim().ToLowerInvariant();
        var pName = (positionName ?? string.Empty).Trim().ToLowerInvariant();

        // 1. Chức danh lãnh đạo / quản lý chung hoặc thực tập: áp dụng được cho mọi phòng ban
        if (pName.Contains("trưởng phòng") || pName.Contains("phó trưởng phòng") ||
            pName.Contains("quản lý") || pName.Contains("thực tập sinh") || pName.Contains("thử việc"))
        {
            return true;
        }

        // 2. Ban Giám Đốc (BGD, BOD)
        if (dCode is "BGD" or "BOD" || dName.Contains("giám đốc") || dName.Contains("ban giám đốc"))
        {
            return pName.Contains("giám đốc") || pName.Contains("trợ lý") ||
                   pName.Contains("administrator") || pName.Contains("quản trị viên");
        }

        // 3. Công nghệ thông tin & Kỹ thuật (IT, TECH)
        if (dCode is "IT" or "TECH" || dName.Contains("công nghệ") || dName.Contains("kỹ thuật") || dName.Contains("cntt"))
        {
            return pName.Contains("developer") || pName.Contains("software") || pName.Contains("kỹ sư") ||
                   pName.Contains("lập trình") || pName.Contains("tester") || pName.Contains("qa") ||
                   pName.Contains("devops") || pName.Contains("hệ thống") || pName.Contains("it");
        }

        // 4. Kinh doanh & Tiếp thị (KD, SALES, MKT)
        if (dCode is "KD" or "SALES" or "MKT" || dName.Contains("kinh doanh") || dName.Contains("tiếp thị") ||
            dName.Contains("bán hàng") || dName.Contains("marketing"))
        {
            return pName.Contains("kinh doanh") || pName.Contains("marketing") || pName.Contains("tiếp thị") ||
                   pName.Contains("bán hàng") || pName.Contains("khách hàng") || pName.Contains("sales") ||
                   pName.Contains("cskh");
        }

        // 5. Kế toán & Tài chính (KT, FIN, ACC)
        if (dCode is "KT" or "FIN" or "ACC" || dName.Contains("kế toán") || dName.Contains("tài chính"))
        {
            return pName.Contains("kế toán") || pName.Contains("tài chính") || pName.Contains("thủ quỹ") ||
                   pName.Contains("ngân quỹ") || pName.Contains("kiểm toán");
        }

        // 6. Nhân sự & Hành chính (HR, NS, ADMIN)
        if (dCode is "HR" or "NS" || dName.Contains("nhân sự") || dName.Contains("hành chính") || dName.Contains("tuyển dụng"))
        {
            return pName.Contains("nhân sự") || pName.Contains("tuyển dụng") || pName.Contains("c&b") ||
                   pName.Contains("đào tạo") || pName.Contains("hành chính");
        }

        // Mặc định các phòng ban đặc thù khác
        return true;
    }
}
