using ImageProcessor.Api.Data;
using ImageProcessor.Api.Model;
using Microsoft.EntityFrameworkCore;

namespace ImageProcessor.Api.Repositories
{
    public interface IUserRepository
    {
        public Task<User?> UserByIdentifier(Guid uid, bool noTracking = false);
        public Task<User?> UserById(Guid uid, bool noTracking = false);
        public Task AddAsync(User user);
    }

    public class UserRepository : IUserRepository
    {
        private readonly DataContext _context;

        public UserRepository(DataContext context) => _context = context;

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task<User?> UserById(Guid uid, bool noTracking = false)
        {
            var users = _context.Users;

            if (noTracking)
                users.AsNoTracking();

            return await users.SingleOrDefaultAsync(d => d.Id == uid);
        }

        public async Task<User?> UserByIdentifier(Guid uid, bool noTracking = false)
        {
            var users = _context.Users;

            if (noTracking)
                users.AsNoTracking();

            return await users.SingleOrDefaultAsync(d => d.UserIdentifier == uid);
        }
    }
}
