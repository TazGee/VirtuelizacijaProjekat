using Common.Models;
using Server.Config;
using Server.Events;
using System;
using System.Collections.Generic;

namespace Server.Analytics
{
    public class ConsumptionAnalytics
    {
        private readonly ServerSettingsReader settings;

        private LoadSample previousSample;
        private int flatlineCounter;
        private bool flatlineWarningAlreadyRaised;

        public ConsumptionAnalytics(ServerSettingsReader settings)
        {
            this.settings = settings;
            previousSample = null;
            flatlineCounter = 0;
            flatlineWarningAlreadyRaised = false;
        }

        public List<WarningEventArgs> AnalyzeBatch(List<LoadSample> samples)
        {
            List<WarningEventArgs> warnings = new List<WarningEventArgs>();

            foreach (LoadSample sample in samples)
            {
                AnalyzeLoadFactor(sample, warnings);
                AnalyzeFlatlineAndSpike(sample, warnings);

                previousSample = sample;
            }

            return warnings;
        }

        private void AnalyzeLoadFactor(LoadSample sample, List<WarningEventArgs> warnings)
        {
            if (sample.ForecastMW == 0)
            {
                return;
            }

            if (double.IsNaN(sample.ActualMW) || double.IsNaN(sample.ForecastMW))
            {
                return;
            }

            double loadFactor = sample.ActualMW / sample.ForecastMW;

            if (loadFactor < settings.LoadFactorMin)
            {
                warnings.Add(new WarningEventArgs(
                    "LowLoadFactorWarning",
                    sample.CountryCode,
                    sample.TimestampLocal.Hour,
                    loadFactor,
                    "",
                    "Low load factor. Hour: " + sample.TimestampLocal.Hour +
                    ", LoadFactor: " + loadFactor.ToString("F3") +
                    ", Country: " + sample.CountryCode));
            }
        }

        private void AnalyzeFlatlineAndSpike(LoadSample sample, List<WarningEventArgs> warnings)
        {
            if (previousSample == null)
            {
                return;
            }

            double delta = sample.ActualMW - previousSample.ActualMW;
            double absoluteDelta = Math.Abs(delta);

            if (absoluteDelta < settings.FlatlineEpsilon)
            {
                flatlineCounter++;

                if (flatlineCounter > settings.FlatlineWindowSamples && !flatlineWarningAlreadyRaised)
                {
                    warnings.Add(new WarningEventArgs(
                        "FlatlineWarning",
                        sample.CountryCode,
                        sample.TimestampLocal.Hour,
                        absoluteDelta,
                        "",
                        "Flatline detected. Difference smaller than " +
                        settings.FlatlineEpsilon +
                        " for more than " +
                        settings.FlatlineWindowSamples +
                        " consecutive samples."));

                    flatlineWarningAlreadyRaised = true;
                }
            }
            else
            {
                flatlineCounter = 0;
                flatlineWarningAlreadyRaised = false;
            }

            if (absoluteDelta > settings.SpikeDeltaMW)
            {
                string direction = delta > 0 ? "UP" : "DOWN";

                warnings.Add(new WarningEventArgs(
                    "ConsumptionSpikeWarning",
                    sample.CountryCode,
                    sample.TimestampLocal.Hour,
                    absoluteDelta,
                    direction,
                    "Consumption spike detected. Direction: " +
                    direction +
                    ", delta: " +
                    absoluteDelta.ToString("F2") +
                    " MW."));
            }
        }
    }
}