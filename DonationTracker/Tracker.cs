using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace DonationTracker
{
    public class Tracker : IDisposable
    {
        private readonly Downloader _downloader;

        private readonly int _teamId;
        private readonly string _donationListFile;
        private readonly string _totalFile;
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("fr-ca");

        private readonly CancellationTokenSource _cancellation;

        private VolatileDouble _teamTotal;

        private Task _statusTask;
        private Task _donationTask;

        public Tracker(int teamId, string donationListFile, string totalFile)
        {
            _downloader = new Downloader();
            _teamId = teamId;
            _donationListFile = donationListFile;
            _totalFile = totalFile;

            _cancellation = new CancellationTokenSource();
        }

        public async Task Start()
        {
            async Task UpdateTeamStatus()
            {
                if(await _downloader.Get($"https://www.extra-life.org/index.cfm?fuseaction=donorDrive.team&teamID={_teamId}&format=json") is JObject json)
                    _teamTotal.Value = json.Value<double>("totalRaisedAmount");
            }

            await UpdateTeamStatus();

            _statusTask = Schedule(TimeSpan.FromMinutes(15), UpdateTeamStatus);

            var participants = await GetParticipants();

            Task<JToken> GetDonationData(int participantId)
            {
                return _downloader.Get($"https://www.extra-life.org/index.cfm?fuseaction=donorDrive.participantDonations&participantID={participantId}&format=json");
            }

            async Task UpdateDonations()
            {
                var donations = from JArray data in await Task.WhenAll(participants.Select(GetDonationData))
                                from JObject item in data
                                select new
                                {
                                    donor = item.Value<string>("donorName") ?? "Anonyme",
                                    amount = item.Value<double?>("donationAmount"),
                                    time = item.Value<DateTime>("createdOn")
                                };

                double total = 0.0;

                using (var writer = new StreamWriter(new FileStream(_donationListFile, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    foreach (var donation in donations.OrderByDescending(x => x.time).Take(5))
                    {
                        writer.Write(donation.donor);

                        if (donation.amount.HasValue)
                        {
                            total += donation.amount.Value;
                            writer.Write(string.Format(Culture, " ({0:C})", donation.amount.Value));
                        }
                        writer.WriteLine();
                    }
                }

                var realTotal = Math.Max(total, _teamTotal.Value);
                using (var writer = new StreamWriter(new FileStream(_totalFile, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    writer.Write(realTotal.ToString("C", Culture));
                }
            }

            await UpdateDonations();

            _donationTask = Schedule(TimeSpan.FromMinutes(1), UpdateDonations);
        }

        private async Task<IList<int>> GetParticipants()
        {
            var json = (JArray)await _downloader.Get($"https://www.extra-life.org/index.cfm?fuseaction=donorDrive.teamParticipants&teamID={_teamId}&format=json");

            return json.OfType<JObject>().Select(x => x.Value<int>("participantID")).ToList();
        }

        private async Task Schedule(TimeSpan update, Func<Task> action)
        {
            var token = _cancellation.Token;

            while (true)
            {
                await Task.Delay(update, token);

                if (token.IsCancellationRequested)
                    return;

                await action();
            }
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            try
            {
                Task.WaitAll(_statusTask, _donationTask);
            }
            catch (TaskCanceledException)
            {
            }
            _downloader.Dispose();
        }
    }
}
