import json
from urllib.request import urlopen
from urllib.parse import urlencode
from collections import defaultdict


class WeatherData:
    def __init__(self):
        self.city = ""
        self.country = ""
        self.current_degree = 0

    def print_data(self):
        print(f"{self.city}, {self.country} {self.current_degree:+}")


class CountryData:
    def __init__(self):
        self.country = ""
        self.cities_number = 0
        self.avg_temp = 0
        self.min_temp = 0
        self.max_temp = 0

    def print_data(self):
        print(
            f"{self.country} - {self.cities_number}, avg: {self.avg_temp:+}, min: {self.min_temp:+}, max: {self.max_temp:+}")


basic_url = "https://wttr.in/"

params = {
    "format": "j1"
}

def fill_weather_datas(cities_: set[str]) -> list:
    result = []
    for city in cities_:
        url = basic_url + city + "?" + urlencode(params)
        with urlopen(url, timeout=5) as response:
            data = json.loads(response.read().decode())

        weather_data_ = WeatherData()
        weather_data_.city = city
        weather_data_.current_degree = int(data["current_condition"][0]["temp_C"])
        weather_data_.country = data["nearest_area"][0]["country"][0]["value"]
        result.append(weather_data_)

    return result

def group_and_count_weather_data_by_country(weather_datas_: list) -> list:
    groups_by_country = defaultdict(list)
    for weather_data_ in weather_datas_:
        groups_by_country[weather_data_.country].append(weather_data_)

    country_datas_ = []
    for country in groups_by_country:
        group_size = len(groups_by_country[country])
        country_data_ = CountryData()
        country_data_.country = country
        country_data_.cities_number = group_size
        country_data_.avg_temp = sum(weather_data_.current_degree for weather_data_ in groups_by_country[country]) / group_size
        country_data_.min_temp = min(weather_data_.current_degree for weather_data_ in groups_by_country[country])
        country_data_.max_temp = max(weather_data_.current_degree for weather_data_ in groups_by_country[country])
        country_datas_.append(country_data_)

    return country_datas_


with open("Cities.txt", "r") as f:
    cities = set(f.read().splitlines())

weather_datas = fill_weather_datas(cities)

for weather_data in weather_datas:
    weather_data.print_data()

country_datas = group_and_count_weather_data_by_country(weather_datas)

for country_data in country_datas:
    country_data.print_data()
