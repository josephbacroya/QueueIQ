import pandas as pd
import numpy as np
from datetime import datetime, timedelta
import random
import os

# Set random seed for reproducibility
np.random.seed(42)
random.seed(42)

# Configuration
NUM_DAYS = 180  # ~6 months
START_DATE = datetime.now() - timedelta(days=NUM_DAYS)

# Service types and their base average durations
SERVICE_TYPES = [
    {"name": "Haircut", "avg_duration": 30, "weight": 0.5},
    {"name": "Buzz Cut", "avg_duration": 15, "weight": 0.2},
    {"name": "Beard Trim", "avg_duration": 20, "weight": 0.15},
    {"name": "Haircut + Beard", "avg_duration": 45, "weight": 0.15}
]
service_names = [s["name"] for s in SERVICE_TYPES]
service_weights = [s["weight"] for s in SERVICE_TYPES]
service_durations = {s["name"]: s["avg_duration"] for s in SERVICE_TYPES}

# Define business hours
OPEN_HOUR = 9
CLOSE_HOUR = 19

def get_staff_on_duty(day_of_week, hour):
    """Simulate staffing levels based on day and time."""
    # Weekend (Sat=5, Sun=6)
    if day_of_week in [5, 6]:
        return 3  # Fully staffed
    
    # Weekday evenings (after 4 PM)
    if hour >= 16:
        return 3
    
    # Weekday mornings/afternoons
    return 2

def get_arrival_rate(day_of_week, hour):
    """Simulate number of customers arriving per hour."""
    base_rate = 2.0
    
    # Weekends are busier
    if day_of_week in [5, 6]:
        base_rate *= 2.0
        
    # Evening rush on weekdays
    if day_of_week < 5 and hour >= 16:
        base_rate *= 1.5
        
    # Lunch rush
    if 11 <= hour <= 13:
        base_rate *= 1.2
        
    return np.random.poisson(base_rate)

def calculate_actual_wait(queue_length, service_name, staff_on_duty):
    """Calculate a realistic wait time with some gaussian noise."""
    avg_duration = service_durations[service_name]
    
    # The estimated wait is roughly the queue length * average duration, divided by staff on duty
    # We add 1 to queue length to include the person themselves in the "work remaining"
    base_wait = ((queue_length + 1) * avg_duration) / staff_on_duty
    
    # Add random variation (standard deviation of 10% of base wait)
    noise = np.random.normal(0, base_wait * 0.1)
    
    actual_wait = max(0, base_wait + noise)
    return round(actual_wait, 1)

def determine_no_show(actual_wait_mins):
    """Probability of no-show increases exponentially with wait time."""
    # Base probability
    prob = 0.02
    
    if actual_wait_mins > 30:
        prob += 0.05
    if actual_wait_mins > 60:
        prob += 0.15
    if actual_wait_mins > 90:
        prob += 0.30
        
    return 1 if random.random() < prob else 0

def generate_data():
    data = []
    
    print(f"Generating synthetic queue data for {NUM_DAYS} days...")
    
    for day_offset in range(NUM_DAYS):
        current_date = START_DATE + timedelta(days=day_offset)
        day_of_week = current_date.weekday()
        
        # Simulate each hour of the day
        queue_length = 0
        
        for hour in range(OPEN_HOUR, CLOSE_HOUR):
            staff_on_duty = get_staff_on_duty(day_of_week, hour)
            num_arrivals = get_arrival_rate(day_of_week, hour)
            
            for _ in range(num_arrivals):
                service_name = np.random.choice(service_names, p=service_weights)
                avg_duration = service_durations[service_name]
                
                actual_wait = calculate_actual_wait(queue_length, service_name, staff_on_duty)
                is_no_show = determine_no_show(actual_wait)
                
                data.append({
                    "ServiceType": service_name,
                    "AvgServiceDurationMins": avg_duration,
                    "DayOfWeek": day_of_week,
                    "HourOfDay": hour,
                    "QueueLengthAtJoin": queue_length,
                    "StaffOnDuty": staff_on_duty,
                    "ActualWaitMinutes": actual_wait,
                    "IsNoShow": is_no_show
                })
                
                # Update queue length for next arrival in this hour
                if is_no_show:
                    # If they no-showed, they eventually left the queue
                    queue_length = max(0, queue_length - 1)
                else:
                    queue_length += 1
            
            # At the end of the hour, the queue clears a bit based on staff processing people
            processed = staff_on_duty * (60 / 30)  # Assume average 30m per customer per staff
            queue_length = max(0, int(queue_length - processed))

    df = pd.DataFrame(data)
    
    # Shuffle the data
    df = df.sample(frac=1).reset_index(drop=True)
    
    output_path = "historical_queue_data.csv"
    df.to_csv(output_path, index=False)
    
    print(f"\nGenerated {len(df)} records.")
    print(f"Data saved to {output_path}")
    print("\nSample Data:")
    print(df.head())
    
    print("\nSummary Statistics:")
    print(f"Average Wait Time: {df['ActualWaitMinutes'].mean():.1f} mins")
    print(f"No-Show Rate: {(df['IsNoShow'].mean() * 100):.1f}%")

if __name__ == "__main__":
    generate_data()
