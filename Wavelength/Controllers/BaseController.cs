using Commons.Models.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Wavelength.Data;

namespace Wavelength.Controllers
{
    /// <summary>
    /// Provides a base controller class that supplies access to the application's database context and common
    /// user-related functionality for derived API controllers.
    /// </summary>
    /// <remarks>This abstract class is intended to be inherited by API controllers that require access to the
    /// application's database context or need to retrieve information about the currently signed-in user. It
    /// centralizes shared logic to promote consistency and reduce code duplication across controllers.</remarks>
    public abstract class BaseController : ControllerBase
    {
        /// <summary>
        /// Gets the default query transformation to apply to user queries before execution.
        /// </summary>
        /// <remarks>This function allows customization of how user queries are filtered, sorted, or
        /// otherwise modified by default. It can be set to apply global query logic, such as filtering out inactive
        /// users or enforcing security constraints. If null, no additional transformation is applied.</remarks>
        protected Func<IQueryable<User>, IQueryable<User>>? DefaultUserQuery { get; private set; }
        
        /// <summary>
        /// Provides access to the application's database context for use by derived classes.
        /// </summary>
        /// <remarks>Intended for use within subclasses to perform database operations. The lifetime and
        /// configuration of the context are managed by the containing class.</remarks>
        protected readonly AppDbContext DbContext;

        /// <summary>
        /// Initializes a new instance of the BaseController class with the specified database context.
        /// </summary>
        /// <param name="dbContext">The database context to be used by the controller. Cannot be null.</param>
        public BaseController(AppDbContext dbContext)
        {
            DbContext = dbContext;
        }

        /// <summary>
        /// Sets the default query transformation to apply to user queries.
        /// </summary>
        /// <remarks>Use this method to specify a default filter, sort order, or other query modifications
        /// that should be applied to all user queries unless explicitly overridden.</remarks>
        /// <param name="configure">A function that takes an <see cref="IQueryable{User}"/> and returns a modified <see
        /// cref="IQueryable{User}"/> representing the default query logic to apply. Cannot be null.</param>
        protected void SetDefaultUserQuery(Func<IQueryable<User>, IQueryable<User>> configure)
        {
            DefaultUserQuery = configure;
        }

        /// <summary>
        /// Asynchronously retrieves the currently signed-in user, applying optional query modifications.
        /// </summary>
        /// <remarks>The base query includes user roles by default. If the user is not signed in or cannot
        /// be found, the method returns null.</remarks>
        /// <param name="query">A function that modifies the base user query. Can be used to include additional related data or apply
        /// filters. If null, no additional modifications are applied.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the signed-in user if found;
        /// otherwise, null.</returns>
        protected async Task<User?> GetSignedInUserAsync(Func<IQueryable<User>, IQueryable<User>>? query = null)
        {
            // Get the user ID from the claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return null;

            // Build the base query
            IQueryable<User> q = DbContext.Users
                .Include(u => u.UserRoles);

            // Default query from configuration
            if (DefaultUserQuery != null)
                q = DefaultUserQuery(q);

            // Apply additional query modifications if provided
            if (query != null)
                q = query(q);

            return await q.FirstOrDefaultAsync(u => u.Id == userId);
        }

    }
}
