// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestModels.ConferencePlanner;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
using ConferenceDTO = Microsoft.EntityFrameworkCore.TestModels.ConferencePlanner.ConferenceDTO;

namespace Microsoft.EntityFrameworkCore
{
    public abstract partial class ConferencePlannerTestBase<TFixture> : IClassFixture<TFixture>
        where TFixture : ConferencePlannerTestBase<TFixture>.ConferencePlannerFixtureBase, new()
    {
        protected ConferencePlannerTestBase(TFixture fixture)
        {
            Fixture = fixture;
            fixture.ListLoggerFactory.Clear();
        }

        [ConditionalFact]
        public virtual async Task AttendeesController_Get()
        {
            await ExecuteWithStrategyInTransactionAsync(
                async context =>
                {
                    var controller = new AttendeesController(context);

                    var attendee = await controller.Get("RainbowDash");

                    Assert.Equal("Rainbow", attendee.FirstName);
                    Assert.Equal("Dash", attendee.LastName);
                    Assert.Equal("RainbowDash", attendee.UserName);
                    Assert.Equal("sonicrainboom@sample.com", attendee.EmailAddress);

                    var sessions = attendee.Sessions;

                    Assert.Equal(21, sessions.Count);
                    Assert.All(sessions, s => Assert.NotEmpty(s.Title));
                });
        }

        [ConditionalFact]
        public virtual async Task AttendeesController_GetSessions()
        {
            await ExecuteWithStrategyInTransactionAsync(
                async context =>
                {
                    var controller = new AttendeesController(context);

                    var sessions = await controller.GetSessions("Princess");

                    Assert.Equal(21, sessions.Count);
                    Assert.All(sessions, s => Assert.NotEmpty(s.Abstract));
                });
        }

        [ConditionalFact]
        public virtual async Task AttendeesController_Post_with_new_attendee()
        {
            await ExecuteWithStrategyInTransactionAsync(
                async context =>
                {
                    var controller = new AttendeesController(context);

                    var result = await controller.Post(
                        new ConferenceDTO.Attendee
                        {
                            EmailAddress = "discord@sample.com", FirstName = "", LastName = "Discord", UserName = "Discord!"
                        });

                    Assert.NotEqual(0, result.Id);
                    Assert.Equal("discord@sample.com", result.EmailAddress);
                    Assert.Equal("", result.FirstName);
                    Assert.Equal("Discord", result.LastName);
                    Assert.Equal("Discord!", result.UserName);
                    Assert.Null(result.Sessions);
                });
        }

        [ConditionalFact]
        public virtual async Task AttendeesController_Post_with_existing_attendee()
        {
            await ExecuteWithStrategyInTransactionAsync(
                async context =>
                {
                    var controller = new AttendeesController(context);

                    var result = await controller.Post(
                        new ConferenceDTO.Attendee
                        {
                            EmailAddress = "pinkie@sample.com", FirstName = "Pinkie", LastName = "Pie", UserName = "Pinks"
                        });

                    Assert.Null(result);
                });
        }

        [ConditionalFact]
        public virtual async Task AttendeesController_AddSession()
        {
            await ExecuteWithStrategyInTransactionAsync(
                async context =>
                {
                    var controller = new AttendeesController(context);

                    var pinky = context.Attendees.Single(a => a.UserName == "Pinks");

                    var pinkySessions = context.Sessions
                        .AsNoTracking()
                        .Where(s => s.SessionAttendees.Any(e => e.Attendee.UserName == "Pinks"))
                        .ToList();

                    var session = context.Sessions.AsNoTracking().Single(e => e.Title == "Hidden gems in .NET Core 3");

                    Assert.Equal(21, pinkySessions.Count);

                    var result = (ConferenceDTO.AttendeeResponse)await controller.AddSession("Pinks", session.Id);

                    Assert.Equal(22, result.Sessions.Count);
                    Assert.Contains(session.Id, result.Sessions.Select(s =>s .Id));

                    Assert.Equal(pinky.Id, result.Id);
                    Assert.Equal(pinky.UserName, result.UserName);
                    Assert.Equal(pinky.FirstName, result.FirstName);
                    Assert.Equal(pinky.LastName, result.LastName);
                    Assert.Equal(pinky.EmailAddress, result.EmailAddress);

                    var existingSessionIds = pinkySessions.Select(s => s.Id).ToList();
                    var newSessionIds = result.Sessions.Select(r => r.Id).ToHashSet();
                    Assert.All(existingSessionIds, i => newSessionIds.Contains(i));

                    Assert.Equal(
                        result.Sessions.Select(r => r.Id).OrderBy(i => i).ToList(),
                        context.Sessions
                            .AsNoTracking()
                            .Where(s => s.SessionAttendees.Any(e => e.Attendee.UserName == "Pinks"))
                            .Select(s => s.Id)
                            .OrderBy(i => i)
                            .ToList());
                });
        }

        [ConditionalFact]
        public virtual async Task AttendeesController_AddSession_bad_session()
        {
            await ExecuteWithStrategyInTransactionAsync(
                async context =>
                {
                    var controller = new AttendeesController(context);

                    var result = (string)await controller.AddSession("Pinks", -777);

                    Assert.Equal("No session", result);
                });
        }

        [ConditionalFact]
        public virtual async Task AttendeesController_AddSession_bad_attendee()
        {
            await ExecuteWithStrategyInTransactionAsync(
                async context =>
                {
                    var controller = new AttendeesController(context);

                    var session = context.Sessions.AsNoTracking().Single(e => e.Title == "Hidden gems in .NET Core 3");

                    var result = (string)await controller.AddSession("The Stig", session.Id);

                    Assert.Equal("No attendee", result);
                });
        }

        protected class AttendeesController
        {
            private readonly ApplicationDbContext _db;

            public AttendeesController(ApplicationDbContext db)
            {
                _db = db;
            }

            public async Task<ConferenceDTO.AttendeeResponse> Get(string username)
            {
                var attendee = await _db.Attendees
                    .Include(a => a.SessionsAttendees)
                    .ThenInclude(sa => sa.Session)
                    .SingleOrDefaultAsync(a => a.UserName == username);

                return attendee?.MapAttendeeResponse();
            }

            public async Task<List<ConferenceDTO.SessionResponse>> GetSessions(string username)
            {
                var sessions = await _db.Sessions.AsNoTracking()
                    .Include(s => s.Track)
                    .Include(s => s.SessionSpeakers)
                    .ThenInclude(ss => ss.Speaker)
                    .Where(s => s.SessionAttendees.Any(sa => sa.Attendee.UserName == username))
                    .Select(m => m.MapSessionResponse())
                    .ToListAsync();

                return sessions;
            }

            public async Task<ConferenceDTO.AttendeeResponse> Post(ConferenceDTO.Attendee input)
            {
                // Check if the attendee already exists
                var existingAttendee = await _db.Attendees
                    .Where(a => a.UserName == input.UserName)
                    .FirstOrDefaultAsync();

                if (existingAttendee != null)
                {
                    return null;
                }

                var attendee = new Attendee
                {
                    FirstName = input.FirstName, LastName = input.LastName, UserName = input.UserName, EmailAddress = input.EmailAddress
                };

                _db.Attendees.Add(attendee);
                await _db.SaveChangesAsync();

                return attendee.MapAttendeeResponse();
            }

            public async Task<object> AddSession(string username, int sessionId)
            {
                var attendee = await _db.Attendees
                    .Include(a => a.SessionsAttendees)
                    .ThenInclude(sa => sa.Session)
                    .SingleOrDefaultAsync(a => a.UserName == username);

                if (attendee == null)
                {
                    return "No attendee";
                }

                var session = await _db.Sessions.FindAsync(sessionId);

                if (session == null)
                {
                    return "No session";
                }

                attendee.SessionsAttendees.Add(
                    new SessionAttendee
                    {
                        AttendeeId = attendee.Id, SessionId = sessionId
                    });

                await _db.SaveChangesAsync();

                return attendee.MapAttendeeResponse();
            }

            public async Task<string> RemoveSession(string username, int sessionId)
            {
                var attendee = await _db.Attendees
                    .Include(a => a.SessionsAttendees)
                    .SingleOrDefaultAsync(a => a.UserName == username);

                if (attendee == null)
                {
                    return "No attendee";
                }

                var session = await _db.Sessions.FindAsync(sessionId);

                if (session == null)
                {
                    return "No session";
                }

                var sessionAttendee = attendee.SessionsAttendees.FirstOrDefault(sa => sa.SessionId == sessionId);
                attendee.SessionsAttendees.Remove(sessionAttendee);

                await _db.SaveChangesAsync();

                return "Success";
            }
        }

        protected TFixture Fixture { get; }

        protected ApplicationDbContext CreateContext() => Fixture.CreateContext();

        protected virtual Task ExecuteWithStrategyInTransactionAsync(
            Func<ApplicationDbContext, Task> testOperation,
            Func<ApplicationDbContext, Task> nestedTestOperation1 = null,
            Func<ApplicationDbContext, Task> nestedTestOperation2 = null,
            Func<ApplicationDbContext, Task> nestedTestOperation3 = null)
            => TestHelpers.ExecuteWithStrategyInTransactionAsync(
                CreateContext,
                UseTransaction,
                testOperation,
                nestedTestOperation1,
                nestedTestOperation2,
                nestedTestOperation3);

        protected virtual void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        {
        }

        public abstract class ConferencePlannerFixtureBase : SharedStoreFixtureBase<ApplicationDbContext>
        {
            protected override string StoreName { get; } = "ConferencePlanner";

            protected override bool UsePooling => false;

            protected override void Seed(ApplicationDbContext context)
            {
                var attendees1 = new List<Attendee>
                {
                    new Attendee
                    {
                        EmailAddress = "sonicrainboom@sample.com", FirstName = "Rainbow", LastName = "Dash", UserName = "RainbowDash"
                    },
                    new Attendee
                    {
                        EmailAddress = "solovely@sample.com", FirstName = "Flutter", LastName = "Shy", UserName = "Fluttershy"
                    }
                };

                var attendees2 = new List<Attendee>
                {
                    new Attendee
                    {
                        EmailAddress = "applesforever@sample.com", FirstName = "Apple", LastName = "Jack", UserName = "Applejack"
                    },
                    new Attendee
                    {
                        EmailAddress = "precious@sample.com", FirstName = "Rarity", LastName = "", UserName = "Rarity"
                    }
                };

                var attendees3 = new List<Attendee>
                {
                    new Attendee
                    {
                        EmailAddress = "princess@sample.com", FirstName = "Twilight", LastName = "Sparkle", UserName = "Princess"
                    },
                    new Attendee
                    {
                        EmailAddress = "pinkie@sample.com", FirstName = "Pinkie", LastName = "Pie", UserName = "Pinks"
                    }
                };

                using var document = JsonDocument.Parse(ConferenceData);

                var tracks = new Dictionary<int, Track>();
                var speakers = new Dictionary<Guid, Speaker>();

                var root = document.RootElement;
                foreach (var dayJson in root.EnumerateArray())
                {
                    foreach (var roomJson in dayJson.GetProperty("rooms").EnumerateArray())
                    {
                        var roomId = roomJson.GetProperty("id").GetInt32();
                        if (!tracks.TryGetValue(roomId, out var track))
                        {
                            track = new Track
                            {
                                Name = roomJson.GetProperty("name").GetString(), Sessions = new List<Session>()
                            };

                            tracks[roomId] = track;
                        }

                        foreach (var sessionJson in roomJson.GetProperty("sessions").EnumerateArray())
                        {
                            var sessionSpeakers = new List<Speaker>();
                            foreach (var speakerJson in sessionJson.GetProperty("speakers").EnumerateArray())
                            {
                                var speakerId = speakerJson.GetProperty("id").GetGuid();
                                if (!speakers.TryGetValue(speakerId, out var speaker))
                                {
                                    speaker = new Speaker
                                    {
                                        Name = speakerJson.GetProperty("name").GetString()
                                    };

                                    speakers[speakerId] = speaker;
                                }

                                sessionSpeakers.Add(speaker);
                            }

                            var session = new Session
                            {
                                Title = sessionJson.GetProperty("title").GetString(),
                                Abstract = sessionJson.GetProperty("description").GetString(),
                                StartTime = sessionJson.GetProperty("startsAt").GetDateTime(),
                                EndTime = sessionJson.GetProperty("endsAt").GetDateTime(),
                            };

                            session.SessionSpeakers = sessionSpeakers.Select(
                                s => new SessionSpeaker
                                {
                                    Session = session, Speaker = s
                                }).ToList();

                            var trackName = track.Name;
                            var attendees = trackName.Contains("1") ? attendees1
                                : trackName.Contains("2") ? attendees2
                                : trackName.Contains("3") ? attendees3
                                : attendees1.Concat(attendees2).Concat(attendees3).ToList();

                            session.SessionAttendees = attendees.Select(
                                a => new SessionAttendee
                                {
                                    Session = session, Attendee = a
                                }).ToList();

                            track.Sessions.Add(session);
                        }
                    }
                }

                context.AddRange(tracks.Values);
                context.SaveChanges();
            }
        }
    }
}
