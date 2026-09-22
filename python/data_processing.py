"""
data_processing.py
-------------------
Data cleaning and preparation utilities used by the prediction module.
Kept separate from prediction.py so cleaning logic can be reused by
future analysis scripts (e.g. ARIMA, Random Forest) without duplication.
"""

import pandas as pd
import numpy as np


def load_history_dataframe(history_records):
    """
    Convert the list of historical record dicts (year, total_births,
    male_births, female_births) received from ASP.NET Core into a
    cleaned Pandas DataFrame ready for modeling.
    """
    df = pd.DataFrame(history_records)

    if df.empty:
        return df

    # Ensure correct dtypes
    df["year"] = pd.to_numeric(df["year"], errors="coerce").astype("Int64")
    for col in ["total_births", "male_births", "female_births"]:
        df[col] = pd.to_numeric(df[col], errors="coerce")

    # Drop rows with missing essential values
    df = df.dropna(subset=["year", "total_births"])

    # Remove impossible negative values
    df = df[(df["total_births"] >= 0)]

    # Collapse duplicate years (average) in case multiple city-level
    # records were aggregated to country level upstream
    df = df.groupby("year", as_index=False).agg({
        "total_births": "sum",
        "male_births": "sum",
        "female_births": "sum"
    })

    df = df.sort_values("year").reset_index(drop=True)

    return df


def compute_gender_ratio(df):
    """
    Returns the historical average male/female share of total births,
    used to split a predicted TotalBirths figure into Male/Female
    estimates when the model only predicts totals directly.
    """
    total_male = df["male_births"].sum()
    total_female = df["female_births"].sum()
    total = total_male + total_female

    if total == 0:
        return 0.512, 0.488  # fallback to a typical global birth sex ratio

    return total_male / total, total_female / total
