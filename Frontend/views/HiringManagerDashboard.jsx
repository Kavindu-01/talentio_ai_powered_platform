import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';

export default function HiringManagerDashboard() {
  const { apiFetch } = useAuth();
  const [interviews, setInterviews] = useState([]);
  const [selectedIv, setSelectedIv] = useState(null);

  // Feedback form state
  const [score, setScore] = useState(70);
  const [feedback, setFeedback] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [success, setSuccess] = useState('');
  const [error, setError] = useState('');

  // Hiring Decision state
  const [deciding, setDeciding] = useState(false);
  
  useEffect(() => {
    fetchInterviews();
  }, []);

  const fetchInterviews = async () => {
    try {
      const res = await apiFetch('interview/my-interviews');
      if (res.ok) {
        const data = await res.json();
        setInterviews(data);
      }
    } catch (e) {
      console.error("Failed to fetch interviews", e);
    }
  };

  const handleFeedbackSubmit = async (e) => {
    e.preventDefault();
    setSubmitting(true);
    setError('');
    setSuccess('');

    try {
      const res = await apiFetch(`interview/${selectedIv.Id}/feedback`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ score: parseInt(score), feedback }),
      });

      const data = await res.json();
      if (!res.ok) {
        throw new Error(data.message || 'Failed to submit feedback');
      }

      setSuccess('Feedback and evaluation score logged successfully!');
      fetchInterviews();
      
      // Update local selection
      setSelectedIv(prev => ({ ...prev, Score: parseInt(score), Feedback: feedback, Status: 'Completed' }));

      setTimeout(() => {
        setSuccess('');
      }, 3000);
    } catch (err) {
      setError(err.message || 'Failed to log feedback');
    } finally {
      setSubmitting(false);
    }
  };

  const handleHiringDecision = async (status) => {
    if (!selectedIv || !selectedIv.JobApplicationId) return;

    setDeciding(true);
    try {
      const res = await apiFetch(`application/${selectedIv.JobApplicationId}/status`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ status }),
      });

      if (res.ok) {
        alert(`Candidate hiring decision set to: ${status}. Notification email dispatched.`);
        fetchInterviews();
        setSelectedIv(null);
      }
    } catch (e) {
      console.error(e);
      alert('Failed to save hiring decision');
    } finally {
      setDeciding(false);
    }
  };

  const handleSelectIv = (iv) => {
    setSelectedIv(iv);
    if (iv.Status === 'Completed') {
      setScore(iv.Score);
      setFeedback(iv.Feedback);
    } else {
      setScore(70);
      setFeedback('');
    }
    setError('');
    setSuccess('');
  };

  return (
    <div>
      <div className="dashboard-header">
        <div className="dashboard-title">
          <h1>Hiring Manager Dashboard</h1>
          <p>Conduct applicant evaluations, record technical scores, submit feedback, and finalize hiring decisions.</p>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '350px 1fr', gap: '2rem' }}>
        
        {/* Left Panel: Assigned Interviews */}
        <div>
          <div className="card" style={{ padding: '1.25rem', minHeight: '60vh' }}>
            <h3 style={{ fontSize: '1.1rem', marginBottom: '1.25rem', color: 'var(--text-secondary)' }}>
              My Scheduled Interviews
            </h3>

            {interviews.length === 0 ? (
              <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', textAlign: 'center', marginTop: '2rem' }}>
                No interviews currently assigned to you.
              </p>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                {interviews.map((iv) => {
                  const isSelected = selectedIv && selectedIv.Id === iv.Id;
                  const isCompleted = iv.Status === 'Completed';

                  return (
                    <div
                      key={iv.Id}
                      style={{
                        padding: '0.8rem',
                        background: isSelected ? 'rgba(59, 130, 246, 0.1)' : 'rgba(255,255,255,0.02)',
                        border: isSelected ? '1px solid var(--primary)' : '1px solid var(--border-color)',
                        borderRadius: '8px',
                        cursor: 'pointer',
                        display: 'flex',
                        flexDirection: 'column',
                        gap: '0.3rem'
                      }}
                      onClick={() => handleSelectIv(iv)}
                    >
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ fontSize: '0.9rem', fontWeight: '600' }}>{iv.CandidateName}</span>
                        <span className={`status-pill status-${iv.Status.toLowerCase()}`} style={{ fontSize: '0.55rem' }}>
                          {iv.Status}
                        </span>
                      </div>
                      <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>{iv.JobTitle}</span>
                      <span style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>
                        {new Date(iv.InterviewDate).toLocaleDateString()} at {new Date(iv.InterviewDate).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>

        {/* Right Panel: Evaluation & Decision */}
        <div>
          {selectedIv ? (
            <div className="card" style={{ minHeight: '60vh' }}>
              <div style={{ borderBottom: '1px solid var(--border-color)', paddingBottom: '1.5rem', marginBottom: '1.5rem' }}>
                <span className={`status-pill status-${selectedIv.Status.toLowerCase()}`} style={{ float: 'right' }}>
                  {selectedIv.Status}
                </span>
                <h2 style={{ fontSize: '1.4rem' }}>Candidate Evaluation: {selectedIv.CandidateName}</h2>
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginTop: '0.25rem' }}>
                  Role: <strong>{selectedIv.JobTitle}</strong> | Interview Date: {new Date(selectedIv.InterviewDate).toLocaleString()}
                </p>
                <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', marginTop: '0.5rem' }}>
                  Location/Meeting Link: <a href={selectedIv.LocationOrLink} target="_blank" rel="noreferrer" style={{ color: 'var(--primary)' }}>{selectedIv.LocationOrLink}</a>
                </p>
              </div>

              {/* Feedback and Scoring Form */}
              <div style={{ marginBottom: '2rem' }}>
                <h3 style={{ fontSize: '1.1rem', marginBottom: '1.25rem', color: 'var(--accent)' }}>
                  {selectedIv.Status === 'Completed' ? 'Interview Feedback (Recorded)' : 'Log Interview Feedback'}
                </h3>

                {error && <div className="alert alert-danger">{error}</div>}
                {success && <div className="alert alert-success">{success}</div>}

                <form onSubmit={handleFeedbackSubmit}>
                  <div className="form-group" style={{ maxWidth: '200px' }}>
                    <label className="form-label" htmlFor="iv-score">Technical Score (0-100)</label>
                    <input
                      id="iv-score"
                      type="number"
                      min="0"
                      max="100"
                      className="form-control"
                      value={score}
                      onChange={(e) => setScore(e.target.value)}
                      disabled={submitting}
                      required
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label" htmlFor="iv-feedback">Evaluation Comments</label>
                    <textarea
                      id="iv-feedback"
                      className="form-control"
                      placeholder="Discuss candidate's technical skills, core competency matching, general fit, and strengths/gaps..."
                      value={feedback}
                      onChange={(e) => setFeedback(e.target.value)}
                      disabled={submitting}
                      required
                    />
                  </div>

                  <button type="submit" className="btn btn-primary" disabled={submitting}>
                    {submitting ? 'Saving...' : 'Save Feedback'}
                  </button>
                </form>
              </div>

              {/* Hiring Decision Management */}
              {selectedIv.Status === 'Completed' && (
                <div style={{ borderTop: '1px solid var(--border-color)', paddingTop: '1.5rem' }}>
                  <h3 style={{ fontSize: '1.1rem', marginBottom: '0.5rem', color: 'var(--success)' }}>
                    Hiring Decision Management
                  </h3>
                  <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '1.25rem' }}>
                    Based on your technical evaluation, submit your recommendation or final decision for this candidate's application.
                  </p>

                  <div style={{ display: 'flex', gap: '1rem' }}>
                    <button
                      className="btn btn-danger"
                      onClick={() => handleHiringDecision('Rejected')}
                      disabled={deciding}
                    >
                      Reject Candidate
                    </button>
                    <button
                      className="btn btn-success"
                      onClick={() => handleHiringDecision('Offered')}
                      disabled={deciding}
                    >
                      Extend Job Offer
                    </button>
                  </div>
                </div>
              )}

            </div>
          ) : (
            <div className="card" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh', color: 'var(--text-muted)' }}>
              Select a scheduled interview from the left panel to record feedback.
            </div>
          )}
        </div>

      </div>
    </div>
  );
}
