namespace Hrm.Contracts;

public static class DepartmentPositionPolicy
{
    /// <summary>
    /// Kiểm tra chức danh/vị trí có tương thích với phòng ban hay không theo cơ chế Data-Driven (Dựa trên quan hệ RDBMS).
    /// 
    /// Quy tắc chuẩn hóa:
    /// 1. Chức danh dùng chung toàn công ty (positionDepartmentId == null hoặc &lt;= 0) -> Áp dụng được cho TẤT CẢ các phòng ban.
    /// 2. Chức danh đặc thù riêng của phòng ban -> Chỉ hợp lệ khi khớp chính xác ID phòng ban (positionDepartmentId == targetDepartmentId).
    /// 
    /// Loại bỏ hoàn toàn việc hardcode chuỗi ký tự bằng if-else, đảm bảo mở rộng linh hoạt theo dữ liệu database.
    /// </summary>
    public static bool IsCompatible(
        long targetDepartmentId,
        long? positionDepartmentId,
        string? deptCode = null,
        string? deptName = null,
        string? positionName = null,
        string? positionCode = null)
    {
        // Chưa chọn phòng ban (xem toàn bộ) -> Luôn hợp lệ
        if (targetDepartmentId <= 0)
        {
            return true;
        }

        // Chức danh dùng chung (dept_id = null hoặc 0) -> Áp dụng cho mọi phòng ban
        if (!positionDepartmentId.HasValue || positionDepartmentId.Value <= 0)
        {
            return true;
        }

        // Chức danh gắn với phòng ban cụ thể -> Phải khớp chính xác ID phòng ban
        return positionDepartmentId.Value == targetDepartmentId;
    }
}
