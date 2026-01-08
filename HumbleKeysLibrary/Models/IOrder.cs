using System;
using System.Collections.Generic;

namespace HumbleKeys.Models
{
    public interface IFileCacheable
    {
        string Buffer { get; set; }
    }
    public interface IOrder : IFileCacheable
    {
        IProduct product { get; set; }
        string gamekey { get; set; }

        string uid { get; set; }

        //string created {get;set;}
        ICollection<ISubProduct> subproducts { get; set; }
        ITpkdDict tpkd_dict { get; set; }
        ICollection<string> path_ids { get; set; }
        int total_choices { get; set; }
        int choices_remaining { get; set; }
        bool ContainsProcessableKeyStatuses();

        // bool ContainsKeyRedemptionStatus(KeyStatus keyStatus);
        // bool ContainsUnredeemedKeys();

        bool IsComplete { get; }
    }

    public enum KeyStatus
    {
        Unclaimed,
        Unredeemed,
        Claimed,
        Redeemed,
        Unredeemable,
        Expired
    }

}