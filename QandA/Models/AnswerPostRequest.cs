using System.ComponentModel.DataAnnotations;
namespace QandA.Models
{
    public class AnswerPostRequest
    {
        [Required]
        // Nullable because an int type defaults to 0 and if we don't pass it in the request will be assigned to 0 and we want it rewuired
        public int? QuestionId { get; set; }
        [Required]
        public string Content { get; set; }
    }
}
