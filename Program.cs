using FluentValidation;
using FluentValidation.Results;
using HOF_API;
using HOF_API.Model;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Web.Http.ModelBinding;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor(); //Requierd Service for Loupe

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();//Swagger enabling
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));//Read connection string from appsettings.json

});
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.All;
    logging.RequestHeaders.Add("sec-ch-ua");
    logging.ResponseHeaders.Add("MyResponseHeader");
    logging.MediaTypeOptions.AddText("application/javascript");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;

});

var app = builder.Build();

//Log all exceptions in Production mode
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseHttpLogging();

// Configure the swagger documentation.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

//Возвращает массив объектов типа Person: [Person, Person, …]
app.MapGet("/v1/persons", async (DataContext context) =>
        {
            try
            {
                return Results.Ok( context.Persons.ToList());
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
            
        }
            );

//Возвращает объект типа Person.
app.MapGet("/v1/person/{id}", async (DataContext context, int id) =>
            await context.Persons.FindAsync(id) is Person person ?
            Results.Ok(person) :
            Results.BadRequest("Person was not found."));

//В теле запроса передавать объект Person. Id должен быть null или undefined.
//Создаёт нового сотрудника в системе с указанными навыками.
app.MapPost("/v1/person", async (DataContext context, PersonData personData) =>
            {
                //Request Validation
                IValidator<PersonData> validator = new PersonDataValidator();
                var validationResult = validator.Validate(personData);
                if (!validationResult.IsValid)
                    return Results.ValidationProblem(validationResult.ToDictionary());
                    
                if (personData!=null && personData.Person.Id > 0)
                    return Results.BadRequest("Person Id must be null or zero");
                Person person = personData.Person;
                var checkPerson = context.Persons.Where(p => p.Name == person.Name &&
                                                     p.DisplayName == person.DisplayName).FirstOrDefault();
                if (checkPerson != null)//The person is already existed
                {
                    return Results.BadRequest("Person is already added");
                }
                context.Persons.Add(person);
                context.SaveChanges();//Saving new person data to database

                var addedPerson = context.Persons.Where(p => p.Name == person.Name &&
                                                         p.DisplayName == person.DisplayName).FirstOrDefault();
                if (addedPerson != null)//The new person has been added successfully
                {
                    List<PerSkill> perSkillList = personData.perSkills;

                    foreach (PerSkill perSkill in perSkillList)
                    {
                        //check if the skill name is already in database
                        var checkSkill = context.Skills.Where(s => s.Name == perSkill.SkillName).FirstOrDefault();
                        if (checkSkill == null)
                        {
                            //Adding new Skill to List of Skills
                            context.Skills.Add(new Skill { Name = perSkill.SkillName });
                            context.SaveChanges();//Saving new skill data to database
                            //Update the value
                            checkSkill = context.Skills.Where(s => s.Name == perSkill.SkillName).FirstOrDefault();
                        }
                        //Now the checkSkill is not null
                        if (checkSkill != null)
                        {
                            var checkPersonSkill = context.PersonSkills.Where(p => p.Skill.Name == checkSkill.Name && p.Person.Equals(person)).FirstOrDefault();
                            if (checkPersonSkill == null)//This person is not linked with this skill yet
                            {
                                context.PersonSkills.Add(new PersonSkill { Person = person, Skill = checkSkill, Level = perSkill.SkillLevel });
                                context.SaveChanges();//Saving new personSkill data to database
                                checkPersonSkill = context.PersonSkills.Where(p => p.Skill.Name == checkSkill.Name && p.Person.Equals(person)).FirstOrDefault();
                            }
                            //Now the checkPersonSkill is not null
                            if (checkPersonSkill != null)
                            {
                                checkPersonSkill.Level = perSkill.SkillLevel;

                                context.SaveChanges();//Saving new skill data to database
                            }
                            else//Some error happened dealing with checkPersonSkill
                            {
                                return Results.StatusCode(500);
                            }
                        }
                        else//Some error happened dealing with the checkSkill
                        {
                            return Results.StatusCode(500);
                        }

                    }
                    context.SaveChanges();
                    return Results.Ok(context.GetPersonData(addedPerson.Id));
                }

                else//Some error happened
                    return Results.StatusCode(500);
            }
            ) ;

//Обновляет данные сотрудника согласно значениям, указанным в объекте Person в теле.
//Обновляет навыки сотрудника согласно указанному набору.
app.MapPut("/v1/person/{id}", async (DataContext context, int id, PersonData personData) =>
            {
                //Request Validation
                IValidator<PersonData> validator = new PersonDataValidator();
                var validationResult = validator.Validate(personData);
                if (!validationResult.IsValid)
                    return Results.ValidationProblem(validationResult.ToDictionary());
                if (id == 0)
                    return Results.BadRequest("id must not be null or zero");

                else
                {
                    Person person = personData.Person;
                    var checkPerson = await context.Persons.FindAsync(id);
                    if (checkPerson == null)//The person is already existed
                    {
                        return Results.BadRequest("Person is not found");
                    }
                    //Updating person info
                    checkPerson.Name = person.Name;
                    checkPerson.DisplayName = person.DisplayName;
                    List<PerSkill> perSkillList = personData.perSkills;
                    foreach (PerSkill perSkill in perSkillList)
                    {
                        //check if the skill name is already in database
                        var checkSkill = context.Skills.Where(s => s.Name == perSkill.SkillName).FirstOrDefault();
                        if (checkSkill == null)
                        {
                            //Adding new Skill to List of Skills
                            context.Skills.Add(new Skill { Name = perSkill.SkillName });
                            context.SaveChanges();//Saving new skill data to database
                                                   //Update the value
                            checkSkill = context.Skills.Where(s => s.Name == perSkill.SkillName).FirstOrDefault();
                        }
                        //Now the checkSkill is not null
                        if (checkSkill != null)
                        {
                            var checkPersonSkill = context.PersonSkills.Where(p => p.Skill.Name == checkSkill.Name && p.Person.Equals(checkPerson)).FirstOrDefault();
                            if (checkPersonSkill == null)//This person is not linked with this skill yet
                            {
                                context.PersonSkills.Add(new PersonSkill
                                {
                                    Person = checkPerson,
                                    Skill = checkSkill,
                                    Level = perSkill.SkillLevel
                                });
                                context.SaveChanges();//Saving new personSkill data to database
                                checkPersonSkill = context.PersonSkills.Where(p => p.Skill.Name == checkSkill.Name && p.Person.Equals(checkPerson)).FirstOrDefault();
                            }
                            //Now the checkPersonSkill is not null
                            if (checkPersonSkill != null)
                            {
                                checkPersonSkill.Level = perSkill.SkillLevel;

                                context.SaveChanges();//Saving new skill data to database
                            }
                            else//Some error happened dealing with checkPersonSkill
                            {
                                return Results.StatusCode(500);
                            }
                        }
                        else//Some error happened dealing with the checkSkill
                        {
                            return Results.StatusCode(500);
                        }

                    }
                    return Results.Ok(context.GetPersonData(id));
                }
            }
            );


//Где id – уникальный идентификатор сотрудника.
//Удаляет с указанным id сотрудника из системы.
app.MapDelete("/v1/person/{id}" , async(DataContext context , int id) =>
            {
                if(id == 0)
                    return Results.BadRequest("Id must not be null");
                var person = await context.Persons.FindAsync(id);
                if (person == null)
                    return Results.BadRequest("Person was not found.");
                else
                {
                    context.Persons.Remove(person);
                    await context.SaveChangesAsync();
                    return Results.Ok(await context.Persons.ToListAsync());
                }
            }
            );
app.Run();

public partial class Program { }
