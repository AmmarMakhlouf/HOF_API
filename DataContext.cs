using HOF_API.Model;
using Microsoft.EntityFrameworkCore;

namespace HOF_API
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<PersonSkill>().HasKey(ps => new { ps.PersonId, ps.SkillId });
            modelBuilder.Entity<PersonSkill>()
                .HasOne<Person>(ps => ps.Person)
                .WithMany();


            modelBuilder.Entity<PersonSkill>()
                .HasOne<Skill>(ps => ps.Skill);
        }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<PersonSkill> PersonSkills { get; set; }

        /// <summary>
        /// Return 
        /// </summary>
        /// <param name="personId"></param>
        /// <returns>
        /// Объект PersonData
        /// </returns>
        public PersonData GetPersonData(int personId)
        {
            var checkPerson = this.Persons.Find(personId);
            if (checkPerson == null)
                return null;
            else
            {
                PersonData result = new PersonData();
                result.perSkills = new List<PerSkill>();
                result.Person = checkPerson;
                List<PersonSkill> personSkills = PersonSkills.Where(ps => ps.Person.Equals(checkPerson)).ToList();
                foreach (PersonSkill perSkill in personSkills)
                {
                    result.perSkills.Add(new PerSkill
                    {
                        SkillId = perSkill.SkillId,
                        SkillName = Skills.Find(perSkill.SkillId) != null ? (Skills.Find(perSkill.SkillId).Name) : "",
                        SkillLevel = perSkill.Level
                    });
                }
                return result;
            }
        }

    }
}
