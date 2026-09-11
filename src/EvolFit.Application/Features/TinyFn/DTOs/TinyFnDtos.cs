namespace EvolFit.Application.Features.TinyFn.DTOs;

// ---------- BMI ----------
public sealed record BmiRequest(double WeightKg, double HeightCm);
public sealed record BmiResponse(decimal Bmi, string Category);

// ---------- BMR ----------
public sealed record BmrRequest(double Weight, double Height, int Age, string Gender);
public sealed record BmrResponse(int Bmr, string Formula, string Unit);

// ---------- TDEE ----------
public sealed record TdeeRequest(double Weight, double Height, int Age, string Gender, string Activity);
public sealed record MacrosSuggestion(int ProteinG, int CarbsG, int FatG);
public sealed record TdeeResponse(
    int Tdee,
    int Bmr,
    double ActivityMultiplier,
    string ActivityLevel,
    string Unit,
    MacrosSuggestion? MacrosSuggestion);

// ---------- Calories ----------
public sealed record CaloriesRequest(double Weight, double Height, int Age, string Gender, string Activity, string Goal);
public sealed record CaloriesResponse(
    int DailyCalories,
    int Tdee,
    int Bmr,
    int Deficit,
    string Goal,
    double WeeklyChangeKg);
