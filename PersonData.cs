using HOF_API.Model;
using System.ComponentModel.DataAnnotations;

namespace HOF_API
{
    //This class has only the skill id, name and level
    public class PerSkill
    {
        public int SkillId { get; set; }
        [Required]
        public string SkillName { get; set; }
        [Range(0, 10)]
        public byte SkillLevel { get; set; }
    }
    public class PersonData
    {
        public Person Person { get; set; }
        public List<PerSkill> perSkills { get; set; }

        /*public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            foreach (PerSkill perSkill in perSkills)
            {
                if (perSkill.SkillName == null) 
                    results.Add(
                    new ValidationResult("Skill name must not be null"));
                if(perSkill.SkillLevel < 0 || perSkill.SkillLevel > 10)
                    results.Add(
                    new ValidationResult("Skill level must be between 0-10"));
            }
            return results;
        }*/
    }
}
