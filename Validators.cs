using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace HOF_API
{
    public class PersonDataValidator : AbstractValidator<PersonData>
    {
        public PersonDataValidator()
        {
            RuleFor(personData => personData.Person)
            .NotNull().WithMessage("Person is Required");
            RuleForEach(personData => personData.perSkills).SetValidator(new PerSkillValidator());

        }
    }

    public class PerSkillValidator : AbstractValidator<PerSkill>
    {
        public PerSkillValidator()
        {
            RuleFor(perSkill => perSkill.SkillName)
            .NotNull().WithMessage("Skill Name is Required");
            RuleFor(perSkill => (int)perSkill.SkillLevel).GreaterThanOrEqualTo(0).LessThanOrEqualTo(10);

        }
    }
}
