using UnityEngine;

public class StoreLevelProgression : MonoBehaviour
{
    private int experiencePoints;

    private int lastProcessedDay;

    private int lastExperienceGained;

    private int lastLevelChange;

    public int ExperiencePoints =>
        experiencePoints;

    public int CurrentLevel =>
        CalculateLevel(
            experiencePoints
        );

    public int LastExperienceGained =>
        lastExperienceGained;

    public int LastLevelChange =>
        lastLevelChange;

    public bool LeveledUpLastProcessing =>
        lastLevelChange > 0;

    public int ExperienceRequiredForNextLevel
    {
        get
        {
            return GetExperienceRequiredForLevel(
                CurrentLevel + 1
            );
        }
    }

    public int ApplyEndOfDayProgression(
        int day,
        int completedGoalCount,
        bool allGoalsCompleted
    )
    {
        day =
            Mathf.Max(
                1,
                day
            );

        if (day <= lastProcessedDay)
        {
            lastExperienceGained =
                0;

            lastLevelChange =
                0;

            return 0;
        }

        int previousLevel =
            CurrentLevel;

        int experienceGained =
            Mathf.Max(
                0,
                completedGoalCount
            );

        if (allGoalsCompleted)
        {
            experienceGained++;
        }

        experiencePoints +=
            experienceGained;

        lastProcessedDay =
            day;

        lastExperienceGained =
            experienceGained;

        lastLevelChange =
            CurrentLevel
            - previousLevel;

        return experienceGained;
    }

    public string GetProgressText()
    {
        int currentLevel =
            CurrentLevel;

        int levelStartExperience =
            GetExperienceRequiredForLevel(
                currentLevel
            );

        int nextLevelExperience =
            GetExperienceRequiredForLevel(
                currentLevel + 1
            );

        int progressIntoLevel =
            experiencePoints
            - levelStartExperience;

        int experienceNeededThisLevel =
            nextLevelExperience
            - levelStartExperience;

        return "STORE LV "
               + currentLevel
               + "  •  "
               + progressIntoLevel
               + " / "
               + experienceNeededThisLevel
               + " XP";
    }

    public StoreLevelSaveData CreateSaveData()
    {
        StoreLevelSaveData saveData =
            new StoreLevelSaveData();

        saveData.experiencePoints =
            experiencePoints;

        saveData.lastProcessedDay =
            lastProcessedDay;

        return saveData;
    }

    public void RestoreFromSaveData(
        StoreLevelSaveData saveData
    )
    {
        experiencePoints =
            0;

        lastProcessedDay =
            0;

        lastExperienceGained =
            0;

        lastLevelChange =
            0;

        if (saveData == null)
        {
            return;
        }

        experiencePoints =
            Mathf.Max(
                0,
                saveData.experiencePoints
            );

        lastProcessedDay =
            Mathf.Max(
                0,
                saveData.lastProcessedDay
            );
    }

    private int CalculateLevel(
        int totalExperience
    )
    {
        totalExperience =
            Mathf.Max(
                0,
                totalExperience
            );

        int level =
            1;

        while (totalExperience
               >= GetExperienceRequiredForLevel(
                   level + 1
               ))
        {
            level++;
        }

        return level;
    }

    private int GetExperienceRequiredForLevel(
        int level
    )
    {
        if (level <= 1)
        {
            return 0;
        }

        return level * level
               + level
               - 2;
    }
}