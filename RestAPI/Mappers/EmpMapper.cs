using RestAPI.DTOs;
using RestAPI.Models;

namespace RestAPI.Mappers
{
    public static class EmpMapper
    {
        // Create DTO → Entity
        public static Emp ToEmp(this EmpCreateDto dto, int userId)
        {
            return new Emp
            {
                Name = dto.empName,
                Designation = dto.empDesignation,
                Email = dto.empEmail,
                UserId = userId // server-side control
            };
        }

        // Entity → Read DTO
        public static EmpReadDto ToEmpReadDto(this Emp emp)
        {
            return new EmpReadDto
            {
                //Id = emp.Id,
                empName = emp.Name,
                empDesignation = emp.Designation,
                empEmail = emp.Email
            };
        }
    }
}
