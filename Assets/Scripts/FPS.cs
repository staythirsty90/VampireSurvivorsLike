
public class FPSData {
    const float fpsMeasurePeriod = 0.5f;
    int m_FpsAccumulator = 0;
    float m_FpsNextPeriod = 0;
    int m_CurrentFps;
    int m_LowestFps = 9999;
    int m_HighestFps = 0;

    const string display = @"{0} avg, {1} high, {2} low";

    public string update(float timeSinceStartUp) {
        m_FpsAccumulator++;
        if(timeSinceStartUp > m_FpsNextPeriod) {
            m_CurrentFps = (int)(m_FpsAccumulator / fpsMeasurePeriod);
            m_FpsAccumulator = 0;
            m_FpsNextPeriod += fpsMeasurePeriod;

            if(m_CurrentFps > m_HighestFps) {
                m_HighestFps = m_CurrentFps;
            }

            if(m_CurrentFps < m_LowestFps) {
                m_LowestFps = m_CurrentFps;
            }
        }
        return string.Format(display, m_CurrentFps, m_HighestFps, m_LowestFps);
    }

    public void reset() {
        m_CurrentFps = 0;
        m_LowestFps = 9999;
        m_HighestFps = 0;
    }
}