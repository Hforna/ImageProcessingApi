using Bogus;
using ImageProcessor.Api.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor.Tests.Fakers
{
    public class UserModelFaker
    {
        public User GenerateRandomUser()
        {
            return new Faker<User>()
                .RuleFor(d => d.Email, f => f.Internet.Email())
                .RuleFor(d => d.UserName, f => f.Name.FullName())
                .RuleFor(d => d.UserIdentifier, Guid.NewGuid())
                .RuleFor(d => d.Id, Guid.NewGuid());
        }
    }
}
