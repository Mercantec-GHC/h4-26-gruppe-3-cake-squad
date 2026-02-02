using System.ComponentModel.DataAnnotations;

namespace Commons.Models.Database
{
	public abstract class Common<T> 
	{
		[Key]
		public T Id { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }

        protected Common()
        {
            if (typeof(T) == typeof(string))
            {
                Id = (T)(object)Guid.NewGuid().ToString("N");
            }
        }
    }
}