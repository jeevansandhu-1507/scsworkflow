using System;
using System.Collections.Generic;
using System.Linq;

namespace SCSPortal.Models;

public class GlMappingResult
{
    public int GlCode { get; set; }
    public string GlName { get; set; } = "";
    public string AccountId { get; set; } = "";
}

public class SupportTypeCompat
{
    public string Name { get; set; } = "";
    public int GlCode { get; set; }
    public string Description { get; set; } = "";
    public List<string> CompatibleFundingTypes { get; set; } = new();
}

public class CommitmentGlMapping
{
    public string FundingType { get; set; } = "";
    public string? PlanType { get; set; }
    public string FundingStatus { get; set; } = "";
    public string SupportType { get; set; } = "";
    public string? PlacementType { get; set; }
    public int GlCode { get; set; }
    public string GlName { get; set; } = "";
    public string AccountId { get; set; } = "";
}

public class InvoiceGlMapping
{
    public string FundingType { get; set; } = "";
    public string FundingStatus { get; set; } = "";
    public string SupportType { get; set; } = "";
    public string? PlacementType { get; set; }
    public int GlCode { get; set; }
    public string GlName { get; set; } = "";
    public string AccountId { get; set; } = "";
}

public static class GlMappingService
{
    // Normalization helper for funding types
    public static string NormalizeFundingType(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        var clean = raw.Trim();
        // Map catalog choices to Excel exact names if different
        if (clean.Equals("Passport Funding", StringComparison.OrdinalIgnoreCase) || 
            clean.Equals("Passport", StringComparison.OrdinalIgnoreCase))
            return "Passport Program";
        
        if (clean.Equals("Temporary Funding - Autism Spectrum Disorder (ASD) Allocation", StringComparison.OrdinalIgnoreCase))
            return "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation"; // excel spelling lacks space after hyphen

        if (clean.Equals("Temporary Funding Allocation - Community Enhancement (CEF)", StringComparison.OrdinalIgnoreCase))
            return "Temporary Funding  Allocation- Community Enhancement (CEF)"; // excel spelling has double space & hyphen space

        if (clean.Equals("Temporary Funding Allocation - Adult", StringComparison.OrdinalIgnoreCase) ||
            clean.Equals("Temporary Funding Allocation (Adult)", StringComparison.OrdinalIgnoreCase))
        {
            var isFrench = SCSPortal.Services.LocaleService.CurrentLocale == AppLocale.Fr;
            return isFrench ? "Temporary Funding Allocation (Adult) - French" : "Temporary Funding Allocation (Adult) - English";
        }

        if (clean.Equals("Temporary Flexible Funding Allocation (Adult)", StringComparison.OrdinalIgnoreCase))
        {
            var isFrench = SCSPortal.Services.LocaleService.CurrentLocale == AppLocale.Fr;
            return isFrench ? "Temporary Flexible Funding  Allocation (Adult) - French" : "Temporary Flexible Funding  Allocation (Adult) - English";
        }

        return clean;
    }

    public static string NormalizePlacementType(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        var clean = raw.Trim();
        if (clean.Equals("None of the above", StringComparison.OrdinalIgnoreCase))
            return "";
        if (clean.Contains("Group Home", StringComparison.OrdinalIgnoreCase) || clean.Contains("Group Living", StringComparison.OrdinalIgnoreCase))
            return "Group Home";
        if (clean.Contains("Host Family", StringComparison.OrdinalIgnoreCase))
            return "Host Family";
        if (clean.Contains("Specialized Accommodations", StringComparison.OrdinalIgnoreCase) || clean.Contains("Specialized Accommodation", StringComparison.OrdinalIgnoreCase))
            return "Specialized Accommodation";
        if (clean.Contains("Supported Independent Living", StringComparison.OrdinalIgnoreCase))
            return "Supported Independent Living (SIL)";
        
        return clean;
    }

    public static List<SupportTypeCompat> SupportTypes { get; } = new()
    {
        new() { Name = "Daily Living Support", GlCode = 5010, Description = "Help with everyday living such as housing costs, meals, supervision, and life-skills support.", CompatibleFundingTypes = new() { "Temporary Funding Allocation - Complex Special Needs (CSN)", "MCCSS Fiscal Residential Funding Allocation", "Passport" } },
        new() { Name = "Individualized Staffing", GlCode = 5020, Description = "Extra staff support at a specific ratio (for example, 1:1 or 2:1) to meet individual needs.", CompatibleFundingTypes = new() { "Temporary Funding Allocation - Complex Special Needs (CSN)", "MCCSS Fiscal Residential Funding Allocation" } },
        new() { Name = "In Home Respite", GlCode = 5110, Description = "Temporary support provided in the person’s home to give a parent or caregiver a break.\nMay include supervision, personal support, or structured activities.", CompatibleFundingTypes = new() { "Temporary Funding Allocation - Children's", "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", "Temporary Funding  Allocation- Community Enhancement (CEF)", "Temporary Funding Allocation - Complex Special Needs (CSN)", "Historical Respite Funding Allocation", "Temporary Funding  Allocation (Adult) - English", "Temporary Funding  Allocation (Adult) - French", "Temporary Flexible Funding  Allocation (Adult) - English", "Temporary Flexible Funding  Allocation (Adult) - French", "MCCSS Fiscal Residential Funding Allocation", "Passport" } },
        new() { Name = "Out of Home Respite", GlCode = 5120, Description = "Temporary care provided outside the person’s home to give a parent or caregiver a break.\nMay include overnight stays, day programs, or supported activities.", CompatibleFundingTypes = new() { "Temporary Funding Allocation - Children's", "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", "Temporary Funding  Allocation- Community Enhancement (CEF)", "Temporary Funding Allocation - Complex Special Needs (CSN)", "Historical Respite Funding Allocation", "Temporary Funding  Allocation (Adult) - English", "Temporary Funding  Allocation (Adult) - French", "Temporary Flexible Funding  Allocation (Adult) - English", "Temporary Flexible Funding  Allocation (Adult) - French", "MCCSS Fiscal Residential Funding Allocation", "MCCSS Fiscal Community Participation Funding Allocation", "Passport" } },
        new() { Name = "Nursing Care", GlCode = 5210, Description = "Health services provided by a registered nurse.", CompatibleFundingTypes = new() { "Temporary Funding Allocation - Complex Special Needs (CSN)", "MCCSS Fiscal Residential Funding Allocation" } },
        new() { Name = "Clinical/Behaviour Supports", GlCode = 5220, Description = "Therapeutic and behavioural services such as psychology, occupational therapy, speech‑language services, and behaviour support.", CompatibleFundingTypes = new() { "Temporary Funding Allocation - Complex Special Needs (CSN)", "MCCSS Fiscal Residential Funding Allocation" } },
        new() { Name = "Assessment", GlCode = 5230, Description = "Clinical, psychosocial, educational, or health assessments used to understand needs and plan supports.", CompatibleFundingTypes = new() { "Temporary Funding Allocation - Complex Special Needs (CSN)", "MCCSS Fiscal Residential Funding Allocation" } },
        new() { Name = "Prescription Medication/Supplies", GlCode = 5240, Description = "Prescription medications and supplies not covered by OHIP, ODSP, or private plans.", CompatibleFundingTypes = new() { "Temporary Funding  Allocation- Community Enhancement (CEF)", "Temporary Funding Allocation - Complex Special Needs (CSN)", "MCCSS Fiscal Residential Funding Allocation" } },
        new() { Name = "Approved Technology/Specialized Equipment", GlCode = 5310, Description = "Devices or equipment that support independence, communication, mobility, or health.", CompatibleFundingTypes = new() { "Temporary Funding  Allocation- Community Enhancement (CEF)", "MCCSS Fiscal Residential Funding Allocation", "Passport" } },
        new() { Name = "Centre Based Day Programming", GlCode = 5510, Description = "Day programs delivered at a central or facility‑based location.", CompatibleFundingTypes = new() { "Historical Respite Funding Allocation", "Temporary Funding  Allocation (Adult) - English", "Temporary Funding  Allocation (Adult) - French", "Temporary Flexible Funding  Allocation (Adult) - English", "Temporary Flexible Funding  Allocation (Adult) - French", "MCCSS Fiscal Community Participation Funding Allocation", "Passport" } },
        new() { Name = "Structured Community Activities", GlCode = 5520, Description = "Ongoing community-based recreational and day programs.", CompatibleFundingTypes = new() { "Temporary Funding Allocation - Children's", "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", "Temporary Funding  Allocation- Community Enhancement (CEF)", "Temporary Funding Allocation - Complex Special Needs (CSN)", "Historical Respite Funding Allocation", "Temporary Funding  Allocation (Adult) - English", "Temporary Funding  Allocation (Adult) - French", "Temporary Flexible Funding  Allocation (Adult) - English", "Temporary Flexible Funding  Allocation (Adult) - French", "MCCSS Fiscal Community Participation Funding Allocation", "Passport" } },
        new() { Name = "Structured Seasonal Programs", GlCode = 5530, Description = "Time‑limited programs such as day camps, summer camps, or seasonal retreats.", CompatibleFundingTypes = new() { "Temporary Funding Allocation - Children's", "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", "MCCSS Fiscal Community Participation Funding Allocation", "Passport" } },
        new() { Name = "Live Events, Admissions & Tickets", GlCode = 5540, Description = "Tickets or admission fees for one‑time events such as concerts, movies, sports events, performances, museums, or attractions.\nDoes not include ongoing programs, camps, or structured day activities.", CompatibleFundingTypes = new() { "Passport" } },
        new() { Name = "Client Travel", GlCode = 5610, Description = "Costs to transport the person receiving support.", CompatibleFundingTypes = new() { "Temporary Funding Allocation - Children's", "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", "Temporary Funding  Allocation- Community Enhancement (CEF)", "Temporary Funding Allocation - Complex Special Needs (CSN)", "Historical Respite Funding Allocation", "Temporary Funding  Allocation (Adult) - English", "Temporary Funding  Allocation (Adult) - French", "Temporary Flexible Funding  Allocation (Adult) - English", "Temporary Flexible Funding  Allocation (Adult) - French", "MCCSS Fiscal Residential Funding Allocation", "MCCSS Fiscal Community Participation Funding Allocation", "Passport" } },
        new() { Name = "Parent Travel", GlCode = 5620, Description = "Transportation costs for parents or caregivers related to supporting the individual.", CompatibleFundingTypes = new() { "Temporary Funding Allocation - Complex Special Needs (CSN)" } },
        new() { Name = "Parent Accommodation (Standard Room Only)", GlCode = 5630, Description = "Overnight lodging for a parent or caregiver when required to support the individual.", CompatibleFundingTypes = new() { "Temporary Funding Allocation - Complex Special Needs (CSN)" } },
        new() { Name = "Support Worker Travel", GlCode = 5640, Description = "Travel costs incurred to get a contracted or agency support worker to the location where support is provided.", CompatibleFundingTypes = new() { "Temporary Funding Allocation - Complex Special Needs (CSN)", "MCCSS Fiscal Residential Funding Allocation", "MCCSS Fiscal Community Participation Funding Allocation", "Passport" } },
        new() { Name = "Fiscal Residential Pressure", GlCode = 5810, Description = "Clinically approved additional funding to address ongoing, exceptional residential support needs for a specific individual.", CompatibleFundingTypes = new() { "MCCSS Fiscal Residential Funding Allocation" } },
        new() { Name = "Fiscal Community Participation Pressure", GlCode = 5820, Description = "Clinically approved additional funding to address ongoing, exceptional community participation support needs for a specific individual.", CompatibleFundingTypes = new() { "MCCSS Fiscal Community Participation Funding Allocation" } },
        new() { Name = "OPR Fire Code Reimbursement", GlCode = 5830, Description = "Reimbursement for fire code–related compliance costs, allocated by Finance.", CompatibleFundingTypes = new() { "MCCSS Fiscal Residential Funding Allocation" } },
        new() { Name = "Passport Administration", GlCode = 5840, Description = "Administrative costs associated with managing PASSPORT funding, within allowable limits.", CompatibleFundingTypes = new() { "Passport" } },
        new() { Name = "Base Increase Adjustment", GlCode = 5850, Description = "Adjustment recorded to reflect confirmed base funding increases from the Ministry.", CompatibleFundingTypes = new() { "MCCSS Fiscal Residential Funding Allocation", "MCCSS Fiscal Community Participation Funding Allocation" } },
        new() { Name = "Permanent Compensation Enhancement", GlCode = 5860, Description = "Finance allocates upon funding confirmation from Ministry", CompatibleFundingTypes = new() { "MCCSS Fiscal Residential Funding Allocation" } },
        new() { Name = "Other Block Program Funding Subsidy", GlCode = 5870, Description = "Funding provided to external organizations to subsidize program delivery, administered by SCS.", CompatibleFundingTypes = new() { "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", "MCCSS Fiscal Community Participation Funding Allocation" } },
        new() { Name = "*Finance Only", GlCode = 0, Description = "", CompatibleFundingTypes = new() {  } },
    };

    public static List<CommitmentGlMapping> CommitmentMappings { get; } = new()
    {
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = "Unrestricted", FundingStatus = "Permanent", SupportType = "N/A", PlacementType = null, GlCode = 5905, GlName = "Unrestricted-MCCSS- Residential", AccountId = "72899000000132992" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Daily Living Support", PlacementType = "Group Home", GlCode = 5011, GlName = "Daily Living Support-GH", AccountId = "72899000000069904" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Daily Living Support", PlacementType = "Host Family", GlCode = 5012, GlName = "Daily Living Support-HF", AccountId = "72899000000125104" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Daily Living Support", PlacementType = "Specialized Accommodation", GlCode = 5013, GlName = "Daily Living Support-SA", AccountId = "72899000000125104" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Daily Living Support", PlacementType = "Supported Independent Living (SIL)", GlCode = 5014, GlName = "Daily Living Support-SIL", AccountId = "72899000000125104" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Individualized Staffing", PlacementType = "Group Home", GlCode = 5041, GlName = "Individualized Staffing-GH", AccountId = "72899000000125104" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Individualized Staffing", PlacementType = "Host Family", GlCode = 5042, GlName = "Individualized Staffing-HF", AccountId = "72899000000125200" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Individualized Staffing", PlacementType = "Specialized Accommodation", GlCode = 5043, GlName = "Individualized Staffing-SA", AccountId = "72899000000125200" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Individualized Staffing", PlacementType = "Supported Independent Living (SIL)", GlCode = 5044, GlName = "Individualized Staffing-SIL", AccountId = "72899000000125200" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Clinical/Behaviour Supports", PlacementType = "Group Home", GlCode = 5221, GlName = "Clinical/Behaviour Supports-GH", AccountId = "72899000000125600" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Clinical/Behaviour Supports", PlacementType = "Host Family", GlCode = 5222, GlName = "Clinical/Behaviour Supports-HF", AccountId = "72899000000125600" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Clinical/Behaviour Supports", PlacementType = "Specialized Accommodation", GlCode = 5223, GlName = "Clinical/Behaviour Supports-SA", AccountId = "72899000000125600" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Clinical/Behaviour Supports", PlacementType = "Supported Independent Living (SIL)", GlCode = 5224, GlName = "Clinical/Behaviour Supports-SIL", AccountId = "72899000000125600" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Client Travel", PlacementType = "Group Home", GlCode = 5611, GlName = "Client Travel-GH", AccountId = "72899000000125504" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Client Travel", PlacementType = "Host Family", GlCode = 5612, GlName = "Client Travel-HF", AccountId = "72899000000125504" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Client Travel", PlacementType = "Specialized Accommodation", GlCode = 5613, GlName = "Client Travel-SA", AccountId = "72899000000125504" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Client Travel", PlacementType = "Supported Independent Living (SIL)", GlCode = 5614, GlName = "Client Travel-SIL", AccountId = "72899000000125504" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Prescription Medication/Supplies", PlacementType = null, GlCode = 5240, GlName = "Prescription Medication/Supplies", AccountId = "72899000000125504" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Prescription Medication/Supplies", PlacementType = "Group Home", GlCode = 5241, GlName = "Prescription Medication/Supplies-GH", AccountId = "72899000000125566" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Prescription Medication/Supplies", PlacementType = "Host Family", GlCode = 5242, GlName = "Prescription Medication/Supplies-HF", AccountId = "72899000000125578" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Prescription Medication/Supplies", PlacementType = "Specialized Accommodation", GlCode = 5243, GlName = "Prescription Medication/Supplies-SA", AccountId = "72899000000125572" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Prescription Medication/Supplies", PlacementType = "Supported Independent Living (SIL)", GlCode = 5244, GlName = "Prescription Medication/Supplies-SIL", AccountId = "72899000000125584" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = null, FundingStatus = "Temporary", SupportType = "Prescription Medication/Supplies", PlacementType = null, GlCode = 5245, GlName = "Prescription Medication/Supplies-CSN", AccountId = "72899000000125590" },
        new() { FundingType = "Temporary Funding  Allocation- Community Enhancement (CEF)", PlanType = null, FundingStatus = "Temporary", SupportType = "Prescription Medication/Supplies", PlacementType = null, GlCode = 5246, GlName = "Prescription Medication/Supplies-CEF for CSN", AccountId = "72899000000125596" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", PlanType = "Unrestricted", FundingStatus = "Permanent", SupportType = "None", PlacementType = null, GlCode = 5910, GlName = "Unrestricted - MCCSS- Community Participation", AccountId = "72899000000319008" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5511, GlName = "Centre Based Day Programming- CP (Perm)", AccountId = "72899000000125200" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5521, GlName = "Structured Community Activities-CP (Perm)", AccountId = "72899000000125200" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5541, GlName = "Structured Seasonal Programs-CP (Perm)", AccountId = "72899000000125296" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Client Travel", PlacementType = null, GlCode = 5616, GlName = "Client Travel-CP (Perm)", AccountId = "72899000000125504" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = "Unrestricted", FundingStatus = "Temporary", SupportType = "Unrestricted Planning Accounts", PlacementType = null, GlCode = 5935, GlName = "Unrestricted Plan-CSN", AccountId = "72899000000376992" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = null, FundingStatus = "Temporary", SupportType = "Daily Living Support", PlacementType = null, GlCode = 5015, GlName = "Daily Living Support-CSN", AccountId = "72899000000125104" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = null, FundingStatus = "Temporary", SupportType = "Individualized Staffing", PlacementType = null, GlCode = 5045, GlName = "Individualized Staffing-CSN", AccountId = "72899000000125104" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = null, FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5114, GlName = "In Home Respite -CSN", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = null, FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5144, GlName = "Out of Home Respite -CSN", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = null, FundingStatus = "Temporary", SupportType = "Nursing Care", PlacementType = null, GlCode = 5210, GlName = "Nursing Care-CSN", AccountId = "72899000000125600" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = null, FundingStatus = "Temporary", SupportType = "Clinical/Behaviour Supports", PlacementType = null, GlCode = 5225, GlName = "Clinical/Behaviour Supports-CSN", AccountId = "72899000000125600" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = null, FundingStatus = "Temporary", SupportType = "Assessments", PlacementType = null, GlCode = 5230, GlName = "Assessments-CSN", AccountId = "72899000000125600" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5525, GlName = "Structured Community Activities-CSN", AccountId = "72899000000372192" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5545, GlName = "Structured Seasonal Programs-CSN", AccountId = "72899000000125296" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = null, FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5615, GlName = "Client Travel-CSN", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = null, FundingStatus = "Temporary", SupportType = "Parent Travel", PlacementType = null, GlCode = 5641, GlName = "Parent Travel-CSN", AccountId = "72899000000125504" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = null, FundingStatus = "Temporary", SupportType = "Support Worker Travel", PlacementType = null, GlCode = 5642, GlName = "Support Worker Travel-CSN", AccountId = "72899000000125504" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", PlanType = null, FundingStatus = "Temporary", SupportType = "Parent Accommodation (Standard Room Only)", PlacementType = null, GlCode = 5635, GlName = "Parent Accommodation (Standard Room Only)-CSN", AccountId = "72899000000347008" },
        new() { FundingType = "Temporary Funding Allocation - Children's", PlanType = "Unrestricted", FundingStatus = "Temporary", SupportType = "None", PlacementType = null, GlCode = 5920, GlName = "Unrestricted Plan - CCM Flex", AccountId = "72899000000343008" },
        new() { FundingType = "Temporary Funding Allocation - Children's", PlanType = null, FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5111, GlName = "In Home Respite -CCM Flex", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding Allocation - Children's", PlanType = null, FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5141, GlName = "Out of Home Respite-CCM Flex", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding Allocation - Children's", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5522, GlName = "Structured Community Activities-CCM Flex", AccountId = "72899000000125200" },
        new() { FundingType = "Temporary Funding Allocation - Children's", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5543, GlName = "Structured Seasonal Programs-CCM Flex", AccountId = "72899000000125296" },
        new() { FundingType = "Temporary Funding Allocation - Children's", PlanType = null, FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5618, GlName = "Client Travel - CCM Flex", AccountId = "72899000000372304" },
        new() { FundingType = "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", PlanType = "Unrestricted", FundingStatus = "Temporary", SupportType = "None", PlacementType = null, GlCode = 5925, GlName = "Unrestricted Plan-ASD", AccountId = "72899000000376992" },
        new() { FundingType = "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", PlanType = null, FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5112, GlName = "In Home Respite - ASD", AccountId = "72899000000372096" },
        new() { FundingType = "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", PlanType = null, FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5142, GlName = "Out of Home Respite -ASD", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", PlanType = null, FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5619, GlName = "Client Travel - ASD", AccountId = "72899000000372304" },
        new() { FundingType = "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5523, GlName = "Structured Community Activities-ASD", AccountId = "72899000000125296" },
        new() { FundingType = "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5542, GlName = "Structured Seasonal Programs-ASD", AccountId = "72899000000125296" },
        new() { FundingType = "Temporary Funding  Allocation- Community Enhancement (CEF)", PlanType = "Unrestricted", FundingStatus = "Temporary", SupportType = "None", PlacementType = null, GlCode = 5930, GlName = "Unrestricted Plan - CEF", AccountId = "72899000000376992" },
        new() { FundingType = "Temporary Funding  Allocation- Community Enhancement (CEF)", PlanType = null, FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5113, GlName = "In Home Respite - CEF", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding  Allocation- Community Enhancement (CEF)", PlanType = null, FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5143, GlName = "Out of Home Respite -CEF", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding  Allocation- Community Enhancement (CEF)", PlanType = null, FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5620, GlName = "Client Travel -CEF", AccountId = "72899000000372304" },
        new() { FundingType = "Temporary Funding  Allocation- Community Enhancement (CEF)", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5524, GlName = "Structured Community  Activities-CEF", AccountId = "72899000000125296" },
        new() { FundingType = "Temporary Funding  Allocation- Community Enhancement (CEF)", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5544, GlName = "Structured Seasonal Programs-CEF", AccountId = "72899000000125296" },
        new() { FundingType = "Historical Respite Funding Allocation", PlanType = "Unrestricted", FundingStatus = "Permanent", SupportType = "None", PlacementType = null, GlCode = 5940, GlName = "Unrestricted Plan-Historical", AccountId = "72899000000376992" },
        new() { FundingType = "Historical Respite Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "In Home Respite", PlacementType = null, GlCode = 5115, GlName = "In Home Respite  - Historical", AccountId = "72899000000133504" },
        new() { FundingType = "Historical Respite Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5145, GlName = "Out of Home Respite -Historical", AccountId = "72899000000125408" },
        new() { FundingType = "Historical Respite Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5512, GlName = "Centre Based Day Programming-Historical", AccountId = "72899000000125200" },
        new() { FundingType = "Historical Respite Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5526, GlName = "Structured Community Activities-Historical", AccountId = "72899000000133504" },
        new() { FundingType = "Historical Respite Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5546, GlName = "Structured Seasonal Programs-Historical", AccountId = "72899000000133504" },
        new() { FundingType = "Historical Respite Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Client Travel", PlacementType = null, GlCode = 5621, GlName = "Client Travel - Historical", AccountId = "72899000000372304" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - English", PlanType = "Unrestricted", FundingStatus = "Temporary", SupportType = "None", PlacementType = null, GlCode = 5945, GlName = "Unrestricted-TSF-E", AccountId = "72899000000376992" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - English", PlanType = null, FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5116, GlName = "In Home Respite -TSF-E", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - English", PlanType = null, FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5146, GlName = "Out of Home Respite -TSF-E", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - English", PlanType = null, FundingStatus = "Temporary", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5513, GlName = "Centre Based Day Programming-TSF-E", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - English", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5527, GlName = "Structured Community Activities-TSF-E", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - English", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5547, GlName = "Structured Seasonal Programs-TSF-E", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - English", PlanType = null, FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5622, GlName = "Client Travel - TSF-E", AccountId = "72899000000372304" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - English", PlanType = "Unrestricted", FundingStatus = "Temporary", SupportType = "Unrestricted Planning Accounts", PlacementType = null, GlCode = 5955, GlName = "Unrestricted Plan-Adult Flex-E", AccountId = "72899000000376992" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - English", PlanType = null, FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5118, GlName = "In Home Respite -Adult Flex-E", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - English", PlanType = null, FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5148, GlName = "Out of Home Respite -Adult Flex-E", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - English", PlanType = null, FundingStatus = "Temporary", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5515, GlName = "Centre Based Day Programming-Adult Flex-E", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - English", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5529, GlName = "Structured Community Activities-Adult  Flex-E", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - English", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5549, GlName = "Structured Seasonal Programs-Adult Flex -E", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - English", PlanType = null, FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5624, GlName = "Client Travel - Adult Flex-E", AccountId = "72899000000372304" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - French", PlanType = "Unrestricted", FundingStatus = "Temporary", SupportType = "None", PlacementType = null, GlCode = 5950, GlName = "Unrestricted Plan-TSF-F", AccountId = "72899000000376992" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - French", PlanType = null, FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5117, GlName = "In Home Respite -TSF-F", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - French", PlanType = null, FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5147, GlName = "Out of Home Respite -TSF-F", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - French", PlanType = null, FundingStatus = "Temporary", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5514, GlName = "Centre Based Day Programming-TSF-F", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - French", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5528, GlName = "Structured Community Activities-TSF-F", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - French", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5548, GlName = "Structured Seasonal Programs-TSF-F", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - French", PlanType = null, FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5623, GlName = "Client Travel - TSF-F", AccountId = "72899000000372304" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - French", PlanType = "Unrestricted", FundingStatus = "Temporary", SupportType = "None", PlacementType = null, GlCode = 5960, GlName = "Unrestricted Plan-Adult Flex-F", AccountId = "72899000000376992" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - French", PlanType = null, FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5119, GlName = "In Home Respite - Adult Flex- F", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - French", PlanType = null, FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5149, GlName = "Out of Home Respite -Adult Flex-F", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - French", PlanType = null, FundingStatus = "Temporary", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5516, GlName = "Centre Based Day Programming-Adult Flex-F", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - French", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5530, GlName = "Structured Community Activities-Adult Flex-F", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - French", PlanType = null, FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5550, GlName = "Structured Seasonal Programs-Adult Flex-F", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - French", PlanType = null, FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5625, GlName = "Client Travel - Adult Flex-F", AccountId = "72899000000372304" },
        new() { FundingType = "Passport Program", PlanType = "Unrestricted", FundingStatus = "Permanent", SupportType = "Unrestricted Planning Accounts", PlacementType = null, GlCode = 5965, GlName = "Unrestricted - Passport", AccountId = "72899000000319008" },
        new() { FundingType = "Passport Program", PlanType = null, FundingStatus = "Permanent", SupportType = "Daily Living Support", PlacementType = null, GlCode = 5016, GlName = "Daily Living Support-Passport", AccountId = "72899000000125104" },
        new() { FundingType = "Passport Program", PlanType = null, FundingStatus = "Permanent", SupportType = "Individualized Staffing", PlacementType = null, GlCode = 5046, GlName = "Individualized Staffing-Passport", AccountId = "72899000000125104" },
        new() { FundingType = "Passport Program", PlanType = null, FundingStatus = "Permanent", SupportType = "In Home Respite", PlacementType = null, GlCode = 5120, GlName = "In Home Respite -Passport", AccountId = "72899000000125408" },
        new() { FundingType = "Passport Program", PlanType = null, FundingStatus = "Permanent", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5150, GlName = "Out of Home Respite -Passport", AccountId = "72899000000125408" },
        new() { FundingType = "Passport Program", PlanType = null, FundingStatus = "Permanent", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5517, GlName = "Centre Based Day Programming-Passport", AccountId = "72899000000125200" },
        new() { FundingType = "Passport Program", PlanType = null, FundingStatus = "Permanent", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5531, GlName = "Structured Community Activities-Passport", AccountId = "72899000000125200" },
        new() { FundingType = "Passport Program", PlanType = null, FundingStatus = "Permanent", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5551, GlName = "Structured Seasonal Programs-Passport", AccountId = "72899000000125296" },
        new() { FundingType = "Passport Program", PlanType = null, FundingStatus = "Permanent", SupportType = "Live Events, Admissions & Tickets", PlacementType = null, GlCode = 5561, GlName = "Live Events & Admissions-Passport", AccountId = "72899000000125296" },
        new() { FundingType = "Passport Program", PlanType = null, FundingStatus = "Permanent", SupportType = "Client Travel", PlacementType = null, GlCode = 5617, GlName = "Client Travel-Passport Program", AccountId = "72899000000125504" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Fiscal Community Participation Pressure", PlacementType = null, GlCode = 5820, GlName = "Fiscal Community Participation Pressure", AccountId = "72899000000125600" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Base Increase Adjustment", PlacementType = null, GlCode = 5850, GlName = "Base Increase Adjustment-Res", AccountId = "72899000000125696" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Permanent Compensation Enhancement", PlacementType = null, GlCode = 5860, GlName = "Permanent Compensation Enhancement-Res", AccountId = "72899000000125696" },
        new() { FundingType = "Passport Program", PlanType = null, FundingStatus = "Permanent", SupportType = "Passport Administration", PlacementType = null, GlCode = 5840, GlName = "Passport Administration", AccountId = "72899000000125696" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Base Increase Adjustment", PlacementType = null, GlCode = 5855, GlName = "Base Increase Adjustment-CP", AccountId = "72899000000376992" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Permanent Compensation Enhancement", PlacementType = null, GlCode = 5865, GlName = "Permanent Compensation Enhancement-CP", AccountId = "72899000000376992" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Block Program Funding Subsidy", PlacementType = null, GlCode = 5870, GlName = "Block Program Funding Subsidy-CP", AccountId = "72899000000347008" },
        new() { FundingType = "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", PlanType = null, FundingStatus = "Temporary", SupportType = "Block Program Funding Subsidy", PlacementType = null, GlCode = 5875, GlName = "Block Program Funding Subsidy-ASD", AccountId = "72899000000376992" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "OPR Fire Code Reimbursement", PlacementType = null, GlCode = 5830, GlName = "OPR Fire Code Reimbursement", AccountId = "72899000000125696" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Admin Fee - Welcome Home", PlacementType = null, GlCode = 5835, GlName = "Admin Fee - Welcome Home", AccountId = "72899000000376992" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", PlanType = null, FundingStatus = "Permanent", SupportType = "Fiscal Residential Pressure", PlacementType = null, GlCode = 5810, GlName = "Fiscal Residential Pressure", AccountId = "72899000000125600" },
    };

    public static List<InvoiceGlMapping> InvoiceMappings { get; } = new()
    {
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Daily Living Support", PlacementType = "Group Home", GlCode = 5011, GlName = "Daily Living Support-GH", AccountId = "72899000000069904" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Daily Living Support", PlacementType = "Host Family", GlCode = 5012, GlName = "Daily Living Support-HF", AccountId = "72899000000125104" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Daily Living Support", PlacementType = "Specialized Accommodation", GlCode = 5013, GlName = "Daily Living Support-SA", AccountId = "72899000000125104" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Daily Living Support", PlacementType = "Supported Independent Living (SIL)", GlCode = 5014, GlName = "Daily Living Support-SIL", AccountId = "72899000000125104" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", FundingStatus = "Temporary", SupportType = "Daily Living Support", PlacementType = null, GlCode = 5015, GlName = "Daily Living Support-CSN", AccountId = "72899000000125104" },
        new() { FundingType = "Passport Program", FundingStatus = "Temporary", SupportType = "Daily Living Support", PlacementType = null, GlCode = 5016, GlName = "Daily Living Support-Passport", AccountId = "72899000000125104" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Individualized Staffing", PlacementType = "Group Home", GlCode = 5041, GlName = "Individualized Staffing-GH", AccountId = "72899000000125104" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Individualized Staffing", PlacementType = "Host Family", GlCode = 5042, GlName = "Individualized Staffing-HF", AccountId = "72899000000125200" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Individualized Staffing", PlacementType = "Specialized Accommodation", GlCode = 5043, GlName = "Individualized Staffing-SA", AccountId = "72899000000125200" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Individualized Staffing", PlacementType = "Supported Independent Living (SIL)", GlCode = 5044, GlName = "Individualized Staffing-SIL", AccountId = "72899000000125200" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", FundingStatus = "Temporary", SupportType = "Individualized Staffing", PlacementType = null, GlCode = 5045, GlName = "Individualized Staffing-CSN", AccountId = "72899000000125104" },
        new() { FundingType = "Passport Program", FundingStatus = "Temporary", SupportType = "Individualized Staffing", PlacementType = null, GlCode = 5046, GlName = "Individualized Staffing-Passport", AccountId = "72899000000125104" },
        new() { FundingType = "Temporary Funding Allocation - Children's", FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5111, GlName = "In Home Respite -CCM Flex", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5112, GlName = "In Home Respite - ASD", AccountId = "72899000000372096" },
        new() { FundingType = "Temporary Funding  Allocation- Community Enhancement (CEF)", FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5113, GlName = "In Home Respite - CEF", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5114, GlName = "In Home Respite -CSN", AccountId = "72899000000125408" },
        new() { FundingType = "Historical Respite Funding Allocation", FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5115, GlName = "In Home Respite  - Historical", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - English", FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5116, GlName = "In Home Respite -TSF-E", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - French", FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5117, GlName = "In Home Respite -TSF-F", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - English", FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5118, GlName = "In Home Respite -Adult Flex-E", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - French", FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5119, GlName = "In Home Respite - Adult Flex- F", AccountId = "72899000000133504" },
        new() { FundingType = "Passport Program", FundingStatus = "Temporary", SupportType = "In Home Respite", PlacementType = null, GlCode = 5120, GlName = "In Home Respite -Passport", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding Allocation - Children's", FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5141, GlName = "Out of Home Respite-CCM Flex", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5142, GlName = "Out of Home Respite -ASD", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding  Allocation- Community Enhancement (CEF)", FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5143, GlName = "Out of Home Respite -CEF", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5144, GlName = "Out of Home Respite -CSN", AccountId = "72899000000125408" },
        new() { FundingType = "Historical Respite Funding Allocation", FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5145, GlName = "Out of Home Respite -Historical", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - English", FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5146, GlName = "Out of Home Respite -TSF-E", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - French", FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5147, GlName = "Out of Home Respite -TSF-F", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - English", FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5148, GlName = "Out of Home Respite -Adult Flex-E", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - French", FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5149, GlName = "Out of Home Respite -Adult Flex-F", AccountId = "72899000000133504" },
        new() { FundingType = "Passport Program", FundingStatus = "Temporary", SupportType = "Out of Home Respite", PlacementType = null, GlCode = 5150, GlName = "Out of Home Respite -Passport", AccountId = "72899000000125408" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", FundingStatus = "Temporary", SupportType = "Nursing Care", PlacementType = null, GlCode = 5210, GlName = "Nursing Care-CSN", AccountId = "72899000000125600" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Clinical/Behaviour Supports", PlacementType = "Group Home", GlCode = 5221, GlName = "Clinical/Behaviour Supports-GH", AccountId = "72899000000125600" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Clinical/Behaviour Supports", PlacementType = "Host Family", GlCode = 5222, GlName = "Clinical/Behaviour Supports-HF", AccountId = "72899000000125600" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Clinical/Behaviour Supports", PlacementType = "Specialized Accommodation", GlCode = 5223, GlName = "Clinical/Behaviour Supports-SA", AccountId = "72899000000125600" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Clinical/Behaviour Supports", PlacementType = "Supported Independent Living (SIL)", GlCode = 5224, GlName = "Clinical/Behaviour Supports-SIL", AccountId = "72899000000125600" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", FundingStatus = "Temporary", SupportType = "Clinical/Behaviour Supports", PlacementType = null, GlCode = 5225, GlName = "Clinical/Behaviour Supports-CSN", AccountId = "72899000000125600" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", FundingStatus = "Temporary", SupportType = "Assessments", PlacementType = null, GlCode = 5230, GlName = "Assessments-CSN", AccountId = "72899000000125600" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", FundingStatus = "Permanent", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5511, GlName = "Centre Based Day Programming- CP (Perm)", AccountId = "72899000000125200" },
        new() { FundingType = "Historical Respite Funding Allocation", FundingStatus = "Temporary", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5512, GlName = "Centre Based Day Programming-Historical", AccountId = "72899000000125200" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - English", FundingStatus = "Temporary", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5513, GlName = "Centre Based Day Programming-TSF-E", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - French", FundingStatus = "Temporary", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5514, GlName = "Centre Based Day Programming-TSF-F", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - English", FundingStatus = "Temporary", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5515, GlName = "Centre Based Day Programming-Adult Flex-E", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - French", FundingStatus = "Temporary", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5516, GlName = "Centre Based Day Programming-Adult Flex-F", AccountId = "72899000000133504" },
        new() { FundingType = "Passport Program", FundingStatus = "Temporary", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5517, GlName = "Centre Based Day Programming-Passport", AccountId = "72899000000125200" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", FundingStatus = "Permanent", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5521, GlName = "Structured Community Activities-CP (Perm)", AccountId = "72899000000125200" },
        new() { FundingType = "Temporary Funding Allocation - Children's", FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5522, GlName = "Structured Community Activities-CCM Flex", AccountId = "72899000000125200" },
        new() { FundingType = "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5523, GlName = "Structured Community Activities-ASD", AccountId = "72899000000125296" },
        new() { FundingType = "Temporary Funding  Allocation- Community Enhancement (CEF)", FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5524, GlName = "Structured Community  Activities-CEF", AccountId = "72899000000125296" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5525, GlName = "Structured Community Activities-CSN", AccountId = "72899000000372192" },
        new() { FundingType = "Historical Respite Funding Allocation", FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5526, GlName = "Structured Community Activities-Historical", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - English", FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5527, GlName = "Structured Community Activities-TSF-E", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - French", FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5528, GlName = "Structured Community Activities-TSF-F", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - English", FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5529, GlName = "Structured Community Activities-Adult  Flex-E", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - French", FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5530, GlName = "Structured Community Activities-Adult Flex-F", AccountId = "72899000000133504" },
        new() { FundingType = "Passport Program", FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5531, GlName = "Structured Community Activities-Passport", AccountId = "72899000000125200" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", FundingStatus = "Permanent", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5541, GlName = "Structured Seasonal Programs-CP (Perm)", AccountId = "72899000000125296" },
        new() { FundingType = "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5542, GlName = "Structured Seasonal Programs-ASD", AccountId = "72899000000125296" },
        new() { FundingType = "Temporary Funding Allocation - Children's", FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5543, GlName = "Structured Seasonal Programs-CCM Flex", AccountId = "72899000000125296" },
        new() { FundingType = "Temporary Funding  Allocation- Community Enhancement (CEF)", FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5544, GlName = "Structured Seasonal Programs-CEF", AccountId = "72899000000125296" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5545, GlName = "Structured Seasonal Programs-CSN", AccountId = "72899000000125296" },
        new() { FundingType = "Historical Respite Funding Allocation", FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5546, GlName = "Structured Seasonal Programs-Historical", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - English", FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5547, GlName = "Structured Seasonal Programs-TSF-E", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - French", FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5548, GlName = "Structured Seasonal Programs-TSF-F", AccountId = "72899000000133600" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - English", FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5549, GlName = "Structured Seasonal Programs-Adult Flex -E", AccountId = "72899000000133504" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - French", FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5550, GlName = "Structured Seasonal Programs-Adult Flex-F", AccountId = "72899000000133504" },
        new() { FundingType = "Passport Program", FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5551, GlName = "Structured Seasonal Programs-Passport", AccountId = "72899000000125296" },
        new() { FundingType = "Passport Program", FundingStatus = "Temporary", SupportType = "Live Events, Admissions & Tickets", PlacementType = null, GlCode = 5561, GlName = "Live Events & Admissions-Passport", AccountId = "72899000000125296" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Client Travel", PlacementType = "Group Home", GlCode = 5611, GlName = "Client Travel-GH", AccountId = "72899000000125504" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Client Travel", PlacementType = "Host Family", GlCode = 5612, GlName = "Client Travel-HF", AccountId = "72899000000125504" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Client Travel", PlacementType = "Specialized Accommodation", GlCode = 5613, GlName = "Client Travel-SA", AccountId = "72899000000125504" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Client Travel", PlacementType = "Supported Independent Living (SIL)", GlCode = 5614, GlName = "Client Travel-SIL", AccountId = "72899000000125504" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5615, GlName = "Client Travel-CSN", AccountId = "72899000000125408" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", FundingStatus = "Permanent", SupportType = "Client Travel", PlacementType = null, GlCode = 5616, GlName = "Client Travel-CP (Perm)", AccountId = "72899000000125504" },
        new() { FundingType = "Passport Program", FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5617, GlName = "Client Travel-Passport Program", AccountId = "72899000000125504" },
        new() { FundingType = "Spec Comm Suppts-Child.-Serv Coord/Case Mgt.", FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5618, GlName = "Client Travel - CCM Flex", AccountId = "72899000000372304" },
        new() { FundingType = "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5619, GlName = "Client Travel - ASD", AccountId = "72899000000372304" },
        new() { FundingType = "Temporary Funding  Allocation- Community Enhancement (CEF)", FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5620, GlName = "Client Travel -CEF", AccountId = "72899000000372304" },
        new() { FundingType = "Historical Respite Funding Allocation", FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5621, GlName = "Client Travel - Historical", AccountId = "72899000000372304" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - English", FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5622, GlName = "Client Travel - TSF-E", AccountId = "72899000000372304" },
        new() { FundingType = "Temporary Funding Allocation (Adult) - French", FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5623, GlName = "Client Travel - TSF-F", AccountId = "72899000000372304" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - English", FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5624, GlName = "Client Travel - Adult Flex-E", AccountId = "72899000000372304" },
        new() { FundingType = "Temporary Flexible Funding  Allocation (Adult) - French", FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5625, GlName = "Client Travel - Adult Flex-F", AccountId = "72899000000372304" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", FundingStatus = "Temporary", SupportType = "Parent Accommodation (Standard Room Only)", PlacementType = null, GlCode = 5635, GlName = "Parent Accommodation (Standard Room Only)-CSN", AccountId = "72899000000347008" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", FundingStatus = "Temporary", SupportType = "Parent Travel", PlacementType = null, GlCode = 5641, GlName = "Parent Travel-CSN", AccountId = "72899000000125504" },
        new() { FundingType = "Temporary Funding Allocation - Complex Special Needs (CSN)", FundingStatus = "Temporary", SupportType = "Support Worker Travel", PlacementType = null, GlCode = 5642, GlName = "Support Worker Travel-CSN", AccountId = "72899000000125504" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Fiscal Residential Pressure", PlacementType = null, GlCode = 5810, GlName = "Fiscal Residential Pressure", AccountId = "72899000000125600" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", FundingStatus = "Permanent", SupportType = "Fiscal Community Participation Pressure", PlacementType = null, GlCode = 5820, GlName = "Fiscal Community Participation Pressure", AccountId = "72899000000125600" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "OPR Fire Code Reimbursement", PlacementType = null, GlCode = 5830, GlName = "OPR Fire Code Reimbursement", AccountId = "72899000000125696" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Admin Fee - Welcome Home", PlacementType = null, GlCode = 5835, GlName = "Admin Fee - Welcome Home", AccountId = "72899000000376992" },
        new() { FundingType = "Passport Program", FundingStatus = "Temporary", SupportType = "Passport Administration", PlacementType = null, GlCode = 5840, GlName = "Passport Administration", AccountId = "72899000000125696" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Base Increase Adjustment", PlacementType = null, GlCode = 5850, GlName = "Base Increase Adjustment-Res", AccountId = "72899000000125696" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", FundingStatus = "Permanent", SupportType = "Base Increase Adjustment", PlacementType = null, GlCode = 5855, GlName = "Base Increase Adjustment-CP", AccountId = "72899000000376992" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Permanent Compensation Enhancement", PlacementType = null, GlCode = 5860, GlName = "Permanent Compensation Enhancement-Res", AccountId = "72899000000125696" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", FundingStatus = "Permanent", SupportType = "Permanent Compensation Enhancement", PlacementType = null, GlCode = 5865, GlName = "Permanent Compensation Enhancement-CP", AccountId = "72899000000376992" },
        new() { FundingType = "MCCSS Fiscal Community Participation Funding Allocation", FundingStatus = "Permanent", SupportType = "Block Program Funding Subsidy", PlacementType = null, GlCode = 5870, GlName = "Block Program Funding Subsidy-CP", AccountId = "72899000000347008" },
        new() { FundingType = "Temporary Funding -Autism Spectrum Disorder (ASD) Allocation", FundingStatus = "Temporary", SupportType = "Block Program Funding Subsidy", PlacementType = null, GlCode = 5875, GlName = "Block Program Funding Subsidy-ASD", AccountId = "72899000000376992" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Prescription Medication/Supplies", PlacementType = "Group Home", GlCode = 5241, GlName = "Prescription Medication/Supplies-GH", AccountId = "72899000000125566" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Prescription Medication/Supplies", PlacementType = "Host Family", GlCode = 5242, GlName = "Prescription Medication/Supplies-HF", AccountId = "72899000000125578" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Prescription Medication/Supplies", PlacementType = "Specialized Accommodation", GlCode = 5243, GlName = "Prescription Medication/Supplies-SA", AccountId = "72899000000125572" },
        new() { FundingType = "MCCSS Fiscal Residential Funding Allocation", FundingStatus = "Permanent", SupportType = "Prescription Medication/Supplies", PlacementType = "Supported Independent Living (SIL)", GlCode = 5244, GlName = "Prescription Medication/Supplies-SIL", AccountId = "72899000000125584" },
        new() { FundingType = "Temporary Funding  Allocation- Community Enhancement (CEF)", FundingStatus = "Temporary", SupportType = "Prescription Medication/Supplies", PlacementType = null, GlCode = 5246, GlName = "Prescription Medication/Supplies-CEF for CSN", AccountId = "72899000000125596" },
        // Base / Generic Program Invoice Mappings (Empty FundingType)
        new() { FundingType = "", FundingStatus = "Temporary", SupportType = "Daily Living Support", PlacementType = null, GlCode = 5010, GlName = "Daily Living Support", AccountId = "72899000000119186" },
        new() { FundingType = "", FundingStatus = "Temporary", SupportType = "Approved Technology/Specialized Equipment", PlacementType = null, GlCode = 5310, GlName = "Approved Technology/Specialized Equipment", AccountId = "72899000000344083" },
        new() { FundingType = "", FundingStatus = "Temporary", SupportType = "Community Participation Supports", PlacementType = null, GlCode = 5500, GlName = "Community Participation Supports", AccountId = "72899000000070057" },
        new() { FundingType = "", FundingStatus = "Temporary", SupportType = "Centre Based Day Programming", PlacementType = null, GlCode = 5510, GlName = "Centre Based Day Programming", AccountId = "72899000000125218" },
        new() { FundingType = "", FundingStatus = "Temporary", SupportType = "Structured Community Activities", PlacementType = null, GlCode = 5520, GlName = "Structured Community Activities", AccountId = "72899000000125266" },
        new() { FundingType = "", FundingStatus = "Temporary", SupportType = "Structured Seasonal Programs", PlacementType = null, GlCode = 5540, GlName = "Structured Seasonal Programs", AccountId = "72899000000347039" },
        new() { FundingType = "", FundingStatus = "Temporary", SupportType = "Live Events, Admissions & Tickets", PlacementType = null, GlCode = 5560, GlName = "Live Events, Admissions & Tickets", AccountId = "72899000000125362" },
        new() { FundingType = "", FundingStatus = "Temporary", SupportType = "Travel & Other Client Costs", PlacementType = null, GlCode = 5600, GlName = "Travel & Other Client Costs", AccountId = "72899000000372297" },
        new() { FundingType = "", FundingStatus = "Temporary", SupportType = "Client Travel", PlacementType = null, GlCode = 5610, GlName = "Client Travel", AccountId = "72899000000125488" },
        new() { FundingType = "", FundingStatus = "Temporary", SupportType = "Parent/Caregiver Travel", PlacementType = null, GlCode = 5640, GlName = "Parent/Caregiver Travel", AccountId = "72899000000125536" },
        new() { FundingType = "", FundingStatus = "Temporary", SupportType = "Extraordinary Supports", PlacementType = null, GlCode = 5800, GlName = "Extraordinary Supports", AccountId = "72899000000377008" },
    };

    public static List<string> GetCompatibleSupportTypes(string fundingType, string planType)
    {
        var normFunding = NormalizeFundingType(fundingType);
        
        if (planType.Equals("Unrestricted", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string> { "Unrestricted Planning Accounts" };
        }

        // Filter SupportTypes list where the header matches or compatible list contains the funding type.
        // We match by name or by normalized representation.
        var list = new List<string>();
        foreach (var st in SupportTypes)
        {
            if (st.CompatibleFundingTypes.Any(c => 
                c.Equals(normFunding, StringComparison.OrdinalIgnoreCase) || 
                NormalizeFundingType(c).Equals(normFunding, StringComparison.OrdinalIgnoreCase) ||
                // check for Passport vs Passport Funding
                (normFunding.Contains("Passport") && c.Contains("Passport"))
            ))
            {
                list.Add(st.Name);
            }
        }
        
        // If the list is empty, default to all restricted support types as fallback, or at least common ones.
        if (list.Count == 0)
        {
            // Just return all non-finance-only support types
            list = SupportTypes.Select(s => s.Name).ToList();
        }
        return list;
    }

    public static GlMappingResult? GetGlMappingForCommitment(string fundingType, string planType, string fundingStatus, string supportType, string? placementType)
    {
        var normFunding = NormalizeFundingType(fundingType);
        
        // CSN / CEF direct lookup bypass logic (ignores placement completely)
        if (normFunding.Equals("Temporary Funding Allocation - Complex Special Needs (CSN)", StringComparison.OrdinalIgnoreCase) ||
            normFunding.Equals("Temporary Funding  Allocation- Community Enhancement (CEF)", StringComparison.OrdinalIgnoreCase))
        {
            CommitmentGlMapping? csnMatch = null;
            if (planType?.Equals("Unrestricted", StringComparison.OrdinalIgnoreCase) == true)
            {
                csnMatch = CommitmentMappings.FirstOrDefault(m =>
                    m.FundingType.Equals(normFunding, StringComparison.OrdinalIgnoreCase) &&
                    m.PlanType != null && m.PlanType.Equals("Unrestricted", StringComparison.OrdinalIgnoreCase)
                );
            }
            else
            {
                csnMatch = CommitmentMappings.FirstOrDefault(m =>
                    m.FundingType.Equals(normFunding, StringComparison.OrdinalIgnoreCase) &&
                    m.SupportType.Equals(supportType, StringComparison.OrdinalIgnoreCase)
                );
            }

            if (csnMatch != null)
            {
                return new GlMappingResult
                {
                    GlCode = csnMatch.GlCode,
                    GlName = csnMatch.GlName,
                    AccountId = csnMatch.AccountId
                };
            }
        }
        if (normFunding.Equals("CHEO", StringComparison.OrdinalIgnoreCase))
        {
            return new GlMappingResult { GlCode = 5970, GlName = "CHEO — Placeholder", AccountId = "CHEO_PLACEHOLDER_ACC_ID" };
        }
        if (normFunding.Equals("SSAH", StringComparison.OrdinalIgnoreCase))
        {
            return new GlMappingResult { GlCode = 5971, GlName = "SSAH — Placeholder", AccountId = "SSAH_PLACEHOLDER_ACC_ID" };
        }
        if (normFunding.Equals("ODSP", StringComparison.OrdinalIgnoreCase))
        {
            return new GlMappingResult { GlCode = 5972, GlName = "ODSP — Placeholder", AccountId = "ODSP_PLACEHOLDER_ACC_ID" };
        }

        var normPlacement = NormalizePlacementType(placementType);
        var normPlan = planType ?? "Restricted";
        var normStatus = fundingStatus ?? "Temporary";

        if (normPlan.Equals("Unrestricted", StringComparison.OrdinalIgnoreCase))
        {
            var unrestrictedMatch = CommitmentMappings.FirstOrDefault(m =>
                m.FundingType.Equals(normFunding, StringComparison.OrdinalIgnoreCase) &&
                m.PlanType != null && m.PlanType.Equals("Unrestricted", StringComparison.OrdinalIgnoreCase) &&
                m.FundingStatus.Equals(normStatus, StringComparison.OrdinalIgnoreCase)
            );
            if (unrestrictedMatch != null)
            {
                return new GlMappingResult
                {
                    GlCode = unrestrictedMatch.GlCode,
                    GlName = unrestrictedMatch.GlName,
                    AccountId = unrestrictedMatch.AccountId
                };
            }
        }

        // Try direct lookup with all fields
        var match = CommitmentMappings.FirstOrDefault(m =>
            m.FundingType.Equals(normFunding, StringComparison.OrdinalIgnoreCase) &&
            (m.PlanType == null || m.PlanType.Equals(normPlan, StringComparison.OrdinalIgnoreCase)) &&
            m.FundingStatus.Equals(normStatus, StringComparison.OrdinalIgnoreCase) &&
            m.SupportType.Equals(supportType, StringComparison.OrdinalIgnoreCase) &&
            (normStatus != "Permanent" || m.PlacementType == null || m.PlacementType.Equals(normPlacement, StringComparison.OrdinalIgnoreCase))
        );

        if (match == null && normStatus == "Permanent")
        {
            // Try lookup without placement type if placement type is null or N/A in DB
            match = CommitmentMappings.FirstOrDefault(m =>
                m.FundingType.Equals(normFunding, StringComparison.OrdinalIgnoreCase) &&
                (m.PlanType == null || m.PlanType.Equals(normPlan, StringComparison.OrdinalIgnoreCase)) &&
                m.FundingStatus.Equals(normStatus, StringComparison.OrdinalIgnoreCase) &&
                m.SupportType.Equals(supportType, StringComparison.OrdinalIgnoreCase) &&
                (m.PlacementType == null)
            );
        }

        if (match != null)
        {
            return new GlMappingResult
            {
                GlCode = match.GlCode,
                GlName = match.GlName,
                AccountId = match.AccountId
            };
        }

        return null;
    }

    public static GlMappingResult? GetGlMappingForInvoice(string fundingType, string fundingStatus, string supportType, string? placementType)
    {
        var normFunding = NormalizeFundingType(fundingType);
        
        // CSN / CEF direct lookup bypass logic for invoices (ignores placement completely)
        if (normFunding.Equals("Temporary Funding Allocation - Complex Special Needs (CSN)", StringComparison.OrdinalIgnoreCase) ||
            normFunding.Equals("Temporary Funding  Allocation- Community Enhancement (CEF)", StringComparison.OrdinalIgnoreCase))
        {
            var csnMatch = InvoiceMappings.FirstOrDefault(m =>
                m.FundingType.Equals(normFunding, StringComparison.OrdinalIgnoreCase) &&
                m.SupportType.Equals(supportType, StringComparison.OrdinalIgnoreCase)
            );

            if (csnMatch != null)
            {
                return new GlMappingResult
                {
                    GlCode = csnMatch.GlCode,
                    GlName = csnMatch.GlName,
                    AccountId = csnMatch.AccountId
                };
            }
        }
        if (normFunding.Equals("CHEO", StringComparison.OrdinalIgnoreCase))
        {
            return new GlMappingResult { GlCode = 5970, GlName = "CHEO — Placeholder", AccountId = "CHEO_PLACEHOLDER_ACC_ID" };
        }
        if (normFunding.Equals("SSAH", StringComparison.OrdinalIgnoreCase))
        {
            return new GlMappingResult { GlCode = 5971, GlName = "SSAH — Placeholder", AccountId = "SSAH_PLACEHOLDER_ACC_ID" };
        }
        if (normFunding.Equals("ODSP", StringComparison.OrdinalIgnoreCase))
        {
            return new GlMappingResult { GlCode = 5972, GlName = "ODSP — Placeholder", AccountId = "ODSP_PLACEHOLDER_ACC_ID" };
        }

        var normPlacement = NormalizePlacementType(placementType);
        var normStatus = fundingStatus ?? "Temporary";

        var match = InvoiceMappings.FirstOrDefault(m =>
            m.FundingType.Equals(normFunding, StringComparison.OrdinalIgnoreCase) &&
            m.FundingStatus.Equals(normStatus, StringComparison.OrdinalIgnoreCase) &&
            m.SupportType.Equals(supportType, StringComparison.OrdinalIgnoreCase) &&
            (normStatus != "Permanent" || m.PlacementType == null || m.PlacementType.Equals(normPlacement, StringComparison.OrdinalIgnoreCase))
        );

        if (match == null && normStatus == "Permanent")
        {
            match = InvoiceMappings.FirstOrDefault(m =>
                m.FundingType.Equals(normFunding, StringComparison.OrdinalIgnoreCase) &&
                m.FundingStatus.Equals(normStatus, StringComparison.OrdinalIgnoreCase) &&
                m.SupportType.Equals(supportType, StringComparison.OrdinalIgnoreCase) &&
                (m.PlacementType == null)
            );
        }

        if (match != null)
        {
            return new GlMappingResult
            {
                GlCode = match.GlCode,
                GlName = match.GlName,
                AccountId = match.AccountId
            };
        }

        return null;
    }
}
