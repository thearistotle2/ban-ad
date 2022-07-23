using BanAd.Config;
using BanAd.Processing.Connectors;
using BanAd.Processing.Entities;
using BanAd.ViewModels;

namespace BanAd.Processing.Workflow;

public class AdSubmissionProcessor
{
    
    private AdSlotsMonitor AdSlots { get; }
    private AdValidator Validator { get; }
    private FileConnector Files { get; }
    private EmailConnector Email { get; }

    public AdSubmissionProcessor(AdSlotsMonitor adSlots, AdValidator validator, FileConnector files, EmailConnector email)
    {
        AdSlots = adSlots;
        Validator = validator;
        Files = files;
        Email  = email;
    }

    internal async Task<AdvertiseResult> ProcessSubmission(AdvertiseInputViewModel model)
    {
        var validation = await Validator.Validate(model);
        if (!validation.Valid)
        {
            return new AdvertiseResult { Success = false, Errors = new string[] { validation.Error } };
        }

        var submitted = Map(model);
        
        // Save ad.
        var (adId, banano) = Files.SaveAdSubmission(submitted);

        // Email submitter and site owner.
        var submitter = Email.SendAdSubmitted(submitted, banano);
        var approver = Email.SendAdForApproval(submitted, adId);
        await Task.WhenAll(submitter, approver);

        return new AdvertiseResult { Success = true };
    }

    internal async Task ProcessApproval(string adSlotId, string adId)
    {
        var submitterEmail = Files.ApproveAdSubmission(adSlotId, adId);
        await Email.SendAdApproved(adSlotId, submitterEmail);
    }

    internal async Task ProcessRejection(string adSlotId, string adId, string? reason)
    {
        var submitterEmail = Files.DeleteAdSubmission(adSlotId, adId);
        await Email.SendAdRejected(adSlotId, submitterEmail, reason);
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
            AdLink = model.Link,
            AdFilename = model.Id + Path.GetExtension(model.Ad.FileName),
            Ad = () => model.Ad.OpenReadStream()
        };
    }
    
    #endregion
    
}