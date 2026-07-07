from fastapi import FastAPI, HTTPException
import yfinance as yf
from datetime import datetime, timedelta

app = FastAPI()

def get_ticker(stock_code: str) -> str:
    """
    判斷是台股還是美股
    台股：純數字 → 加上 .TW（例如 0050 → 0050.TW）
    美股：英文字母 → 直接用（例如 AAPL → AAPL）
    """
    if stock_code.isdigit():
        return f"{stock_code}.TW"
    return stock_code.upper()


@app.get("/price/today")
def get_today_price(stock_code: str):
    """
    取得今日股價
    用法：GET /price/today?stock_code=0050
          GET /price/today?stock_code=AAPL
    """
    ticker = get_ticker(stock_code)
    stock = yf.Ticker(ticker)

    # 抓最近5天，避免今天休市沒資料
    hist = stock.history(period="5d")

    if hist.empty:
        raise HTTPException(status_code=404, detail=f"找不到 {ticker} 的股價資料")

    # 取最新一筆收盤價
    latest = hist.iloc[-1]
    latest_date = hist.index[-1].strftime("%Y-%m-%d")

    return {
        "stock_code": stock_code,
        "ticker": ticker,
        "date": latest_date,
        "close": round(float(latest["Close"]), 2)
    }


@app.get("/price/history")
def get_history_price(stock_code: str, date: str):
    """
    取得指定日期的股價
    用法：GET /price/history?stock_code=0050&date=2024-01-15
    如果指定日期是假日/休市，會取最近的前一個交易日
    """
    ticker = get_ticker(stock_code)
    stock = yf.Ticker(ticker)

    try:
        target_date = datetime.strptime(date, "%Y-%m-%d")
    except ValueError:
        raise HTTPException(status_code=400, detail="日期格式錯誤，請用 yyyy-MM-dd")

    # 抓目標日期前後10天，確保能找到最近的交易日
    start = (target_date - timedelta(days=10)).strftime("%Y-%m-%d")
    end = (target_date + timedelta(days=1)).strftime("%Y-%m-%d")

    hist = stock.history(start=start, end=end)

    if hist.empty:
        raise HTTPException(status_code=404, detail=f"找不到 {ticker} 在 {date} 附近的股價資料")

    # 過濾掉目標日期之後的資料，取最近的交易日
    hist.index = hist.index.tz_localize(None)
    hist = hist[hist.index <= target_date]

    if hist.empty:
        raise HTTPException(status_code=404, detail=f"找不到 {ticker} 在 {date} 之前的股價資料")

    latest = hist.iloc[-1]
    actual_date = hist.index[-1].strftime("%Y-%m-%d")

    return {
        "stock_code": stock_code,
        "ticker": ticker,
        "requested_date": date,
        "actual_date": actual_date,  # 實際找到的交易日（可能跟requested_date不同）
        "close": round(float(latest["Close"]), 2)
    }


@app.get("/price/batch")
def get_batch_prices(stock_code: str, dates: str):
    """
    一次取得多個日期的股價（避免多次呼叫 API）
    dates 用逗號分隔：2024-01-15,2024-03-20,2024-06-10
    用法：GET /price/batch?stock_code=0050&dates=2024-01-15,2024-03-20
    """
    ticker = get_ticker(stock_code)
    stock = yf.Ticker(ticker)

    date_list = dates.split(",")

    try:
        parsed_dates = [datetime.strptime(d.strip(), "%Y-%m-%d") for d in date_list]
    except ValueError:
        raise HTTPException(status_code=400, detail="日期格式錯誤，請用 yyyy-MM-dd")

    # 抓最早到最晚日期的整段資料，減少 API 呼叫次數
    start = (min(parsed_dates) - timedelta(days=10)).strftime("%Y-%m-%d")
    end = (max(parsed_dates) + timedelta(days=1)).strftime("%Y-%m-%d")

    hist = stock.history(start=start, end=end)

    if hist.empty:
        raise HTTPException(status_code=404, detail=f"找不到 {ticker} 的股價資料")

    hist.index = hist.index.tz_localize(None)

    results = []
    for target_date in parsed_dates:
        filtered = hist[hist.index <= target_date]
        if filtered.empty:
            results.append({
                "requested_date": target_date.strftime("%Y-%m-%d"),
                "actual_date": None,
                "close": None,
                "error": "找不到資料"
            })
        else:
            latest = filtered.iloc[-1]
            results.append({
                "requested_date": target_date.strftime("%Y-%m-%d"),
                "actual_date": filtered.index[-1].strftime("%Y-%m-%d"),
                "close": round(float(latest["Close"]), 2)
            })

    return {
        "stock_code": stock_code,
        "ticker": ticker,
        "prices": results
    }
@app.get("/price/today/batch")
def get_today_batch_price(stock_codes: str):
    """
    一次取得多支股票今日股價
    stock_codes 用逗號分隔：0050,2330,AAPL
    用法：GET /price/today/batch?stock_codes=0050,2330
    """
    code_list = stock_codes.split(",")
    results = []

    for code in code_list:
        ticker_str = get_ticker(code.strip())
        try:
            stock = yf.Ticker(ticker_str)
            hist = stock.history(period="5d")

            if hist.empty:
                results.append({
                    "stock_code": code.strip(),
                    "ticker": ticker_str,
                    "date": None,
                    "close": None,
                    "error": "找不到資料"
                })
            else:
                latest = hist.iloc[-1]
                latest_date = hist.index[-1].strftime("%Y-%m-%d")
                results.append({
                    "stock_code": code.strip(),
                    "ticker": ticker_str,
                    "date": latest_date,
                    "close": round(float(latest["Close"]), 2),
                    "error": None
                })
        except Exception as e:
            results.append({
                "stock_code": code.strip(),
                "ticker": ticker_str,
                "date": None,
                "close": None,
                "error": str(e)
            })

    return {"prices": results}

# main.py 最底部加上
if __name__ == "__main__":
    import uvicorn
    import os
    port = int(os.environ.get("PORT", 8000))
    uvicorn.run("main:app", host="0.0.0.0", port=port)