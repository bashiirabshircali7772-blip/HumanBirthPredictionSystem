"""
prediction.py
--------------
Python Prediction Module for the Human Birth Prediction and Gender
Distribution System.

Workflow (invoked by Services/PythonPredictionService.cs):
  1. Read a JSON payload from stdin containing:
       - start_year, end_year: the prediction window (e.g. 2025-2035)
       - history: a list of {year, total_births, male_births, female_births}
         historical records retrieved from SQL Server
  2. Clean and prepare the dataset with Pandas (data_processing.py).
  3. Train a Linear Regression model (scikit-learn) on Year -> TotalBirths.
  4. Predict TotalBirths for every year in [start_year, end_year].
  5. Split each predicted total into Male/Female estimates using the
     historical average gender ratio.
  6. Write a single JSON object to stdout describing the outcome.

This module is intentionally independent of any particular caller: it
communicates purely via stdin/stdout JSON, so it can be re-used by other
tools, tested directly from the command line, or swapped for a different
model (Polynomial Regression, Random Forest, ARIMA, ...) by adding a new
function and changing MODEL_NAME / fit_and_predict() below.
"""

import sys
import json

import numpy as np
from sklearn.linear_model import LinearRegression

from data_processing import load_history_dataframe, compute_gender_ratio

MODEL_NAME = "Linear Regression"


def fit_and_predict(df, start_year, end_year):
    """
    Train a Linear Regression model on historical Year -> TotalBirths
    data and predict totals for every year in the requested range.

    Returns a dict: {year: predicted_total_births}
    """
    X = df["year"].to_numpy().reshape(-1, 1).astype(float)
    y = df["total_births"].to_numpy().astype(float)

    model = LinearRegression()
    model.fit(X, y)

    years_to_predict = np.arange(start_year, end_year + 1).reshape(-1, 1).astype(float)
    predicted = model.predict(years_to_predict)

    # Predictions should never be negative
    predicted = np.clip(predicted, a_min=0, a_max=None)

    return {
        int(year[0]): int(round(value))
        for year, value in zip(years_to_predict, predicted)
    }


def main():
    raw_input = sys.stdin.read()

    try:
        payload = json.loads(raw_input)
    except json.JSONDecodeError as exc:
        print(json.dumps({"success": False, "error": f"Invalid input JSON: {exc}"}))
        return

    start_year = payload.get("start_year")
    end_year = payload.get("end_year")
    history_records = payload.get("history", [])

    if start_year is None or end_year is None:
        print(json.dumps({"success": False, "error": "start_year and end_year are required."}))
        return

    if start_year > end_year:
        print(json.dumps({"success": False, "error": "start_year must be less than or equal to end_year."}))
        return

    df = load_history_dataframe(history_records)

    if df.empty or len(df) < 2:
        print(json.dumps({
            "success": False,
            "error": "At least two valid years of historical data are required to train the prediction model."
        }))
        return

    try:
        totals_by_year = fit_and_predict(df, start_year, end_year)
        male_ratio, female_ratio = compute_gender_ratio(df)

        predictions = []
        for year in range(start_year, end_year + 1):
            total = totals_by_year[year]
            male = int(round(total * male_ratio))
            female = total - male
            predictions.append({
                "year": year,
                "total_births": total,
                "male_births": male,
                "female_births": female
            })

        print(json.dumps({
            "success": True,
            "model": MODEL_NAME,
            "predictions": predictions
        }))

    except Exception as exc:  # noqa: BLE001 - surface any modeling error to caller
        print(json.dumps({"success": False, "error": f"Prediction failed: {exc}"}))


if __name__ == "__main__":
    main()
