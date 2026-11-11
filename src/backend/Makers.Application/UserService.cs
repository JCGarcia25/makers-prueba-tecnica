using System.ComponentModel.DataAnnotations;
using Makers.Domain;

namespace Makers.Application;

public sealed class UserService(IUserStore store, IPasswordService passwords)
{
    public async Task<User> CreateAsync(UserInput input, CancellationToken ct)
    {
        var email = NormalizeEmail(input.Email);
        var name = ValidateName(input.Name);
        if (string.IsNullOrEmpty(input.Password) || input.Password.Length is < 10 or > 128)
            throw new BusinessException("La contraseña debe tener entre 10 y 128 caracteres.");
        if (await store.FindByEmailAsync(email, ct) is not null)
            throw new BusinessException("El correo ya está registrado.", 409);
        var user = new User { Email = email, Name = name };
        user.PasswordHash = passwords.Hash(user, input.Password);
        await store.SaveAsync(user, true, ct);
        return user;
    }

    public async Task<UserView> UpdateAsync(Guid id, UserUpdate input, CancellationToken ct)
    {
        var user = await store.FindAsync(id, ct) ?? throw new BusinessException("Usuario no encontrado.", 404);
        user.Email = NormalizeEmail(input.Email);
        user.Name = ValidateName(input.Name);
        user.SecurityVersion = Guid.NewGuid();
        await store.SaveAsync(user, false, ct);
        return View(user);
    }

    public async Task DeactivateAsync(Guid id, Guid currentId, CancellationToken ct)
    {
        var user = await store.FindAsync(id, ct) ?? throw new BusinessException("Usuario no encontrado.", 404);
        if (id == currentId || user.Role == "Admin")
            throw new BusinessException("No se permite desactivar administradores.", 409);
        user.IsActive = false;
        user.SecurityVersion = Guid.NewGuid();
        await store.SaveAsync(user, false, ct);
    }

    public static UserView View(User user) => new(user.Id, user.Email, user.Name, user.Role, user.IsActive);

    private static string NormalizeEmail(string? email)
    {
        var value = email?.Trim().ToLowerInvariant() ?? "";
        if (value.Length > 254 || !new EmailAddressAttribute().IsValid(value))
            throw new BusinessException("El correo no es válido.");
        return value;
    }

    private static string ValidateName(string? name)
    {
        var value = name?.Trim() ?? "";
        if (value.Length is < 2 or > 100) throw new BusinessException("El nombre debe tener entre 2 y 100 caracteres.");
        return value;
    }
}
