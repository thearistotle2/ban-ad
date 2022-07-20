using BanAd.Config;
using BanAd.Processing.Connectors;
using BanAd.Processing.Entities;
using BanAd.ViewModels;

namespace BanAd.Processing.Workflow;

public class AdSubmissionProcessor
{
    
    private AdSlotsMonitor AdSlots { get; }
    private AdValidator Validator { get; }
    private EmailConnector Email { get; }

    public AdSubmissionProcessor(AdSlotsMonitor adSlots, AdValidator validator, EmailConnector email)
    {
        AdSlots = adSlots;
        Validator = validator;
        Email  = email;
    }

    public async Task<AdvertiseResult> ProcessSubmission(AdvertiseInputViewModel model)
    {
        var validation = await Validator.Validate(model);
        if (!validation.Valid)
        {
            return new AdvertiseResult { Success = false, Errors = new string[] { validation.Error } };
        }

        var submitted = Map(model);
        
        // Save ad.

        // Email submitter and site owner.
        var submitter = Email.SendAdSubmitted(submitted);
        
        await Task.WhenAll(submitter);

        return new AdvertiseResult { Success = true };
    }
    
    #region " Map "

    private SubmittedAd Map(AdvertiseInputViewModel model)
    {
        var hours = (model.Days ?? 0) * 24 + model.Hours;
        return new SubmittedAd
        {
            AdSlotId = model.Id,
            Hours = hours,
            Banano = hours * AdSlots.Value.Ads[model.Id].BanPerHour,
            SubmitterEmail = model.Email,
            AdFilename = model.Id + Path.GetExtension(model.Ad.FileName),
            Ad = () => model.Ad.OpenReadStream()
        };
    }
    
    #endregion
    
}