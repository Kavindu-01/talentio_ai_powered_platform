import React, { useState, useEffect } from 'react';
import { AuthProvider, useAuth } from './context/AuthContext';
import Login from './views/Login';
import Register from './views/Register';
import CandidatePortal from './views/CandidatePortal';
import RecruiterPortal from './views/RecruiterPortal';
import HiringManagerDashboard from './views/HiringManagerDashboard';
import AdminPortal from './views/AdminPortal';

import ProfileModal from './components/ProfileModal';

function MainAppContent() {
  const { user, logout, loading } = useAuth();
  const [showRegister, setShowRegister] = useState(false);
  const [showProfileModal, setShowProfileModal] = useState(false);

  useEffect(() => {
    const handleOpen = () => setShowProfileModal(true);
    window.addEventListener('open-profile-modal', handleOpen);
    return () => window.removeEventListener('open-profile-modal', handleOpen);
  }, []);

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh', backgroundColor: '#0B0F19', color: 'white' }}>
        <h3>Loading Platform Session...</h3>
      </div>
    );
  }

  // Auth flow if user is not logged in
  if (!user) {
    if (showRegister) {
      return <Register onSwitchToLogin={() => setShowRegister(false)} />;
    }
    return <Login onSwitchToRegister={() => setShowRegister(true)} />;
  }

  // Map user role string representation to localized display tag
  const roleDisplayNames = {
    'Administrator': 'System Admin',
    'Recruiter': 'Recruitment Consultant',
    'HiringManager': 'Hiring Manager',
    'Candidate': 'Candidate Account'
  };

  return (
    <div className="app-container">
      {/* Navigation Header */}
      <header role="banner">
        <nav className="navbar" role="navigation" aria-label="Main Navigation">
          <a href="/" className="nav-brand" onClick={(e) => e.preventDefault()} aria-label="Talentio Home">
            <svg style={{ width: '24px', height: '24px', fill: 'var(--primary)' }} viewBox="0 0 24 24" aria-hidden="true">
              <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z" />
            </svg>
            <span>Talentio</span>
          </a>

          <div className="nav-links">
            <div
              className="user-badge"
              onClick={() => setShowProfileModal(true)}
              onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); setShowProfileModal(true); } }}
              tabIndex={0}
              role="button"
              aria-label={`User Profile: ${user.name}. Click or press Enter to customize account profile.${user.role === 'Candidate' ? ' and build resume.' : ''}`}
              style={{ cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '0.6rem' }}
              title={user.role === 'Candidate' ? 'Click to customize profile & build resume' : 'Click to customize account profile'}
            >
              {user.avatarUrl ? (
                <img
                  src={user.avatarUrl}
                  alt={`${user.name}'s profile avatar`}
                  style={{ width: '30px', height: '30px', borderRadius: '50%', objectFit: 'cover', border: '1.5px solid var(--primary)' }}
                />
              ) : (
                <div style={{
                  width: '30px', height: '30px', borderRadius: '50%', background: 'var(--primary)',
                  color: 'white', display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontSize: '0.85rem', fontWeight: 'bold'
                }}>
                  {user.name ? user.name[0].toUpperCase() : 'U'}
                </div>
              )}
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                <span style={{ fontWeight: '600' }}>{user.name}</span>
                <span className={`role-tag ${user.role.toLowerCase() === 'administrator' ? 'admin' : user.role.toLowerCase() === 'hiringmanager' ? 'manager' : user.role.toLowerCase() === 'recruiter' ? 'recruiter' : ''}`}>
                  {roleDisplayNames[user.role] || user.role}
                </span>
              </div>
            </div>

            <button className="btn btn-secondary btn-sm" onClick={logout} aria-label="Sign Out of Application">
              Sign Out
            </button>
          </div>
        </nav>
      </header>

      <ProfileModal isOpen={showProfileModal} onClose={() => setShowProfileModal(false)} />

      {/* Render appropriate dashboard based on role */}
      <main className="main-content">
        {user.role === 'Candidate' && <CandidatePortal />}
        {user.role === 'Recruiter' && <RecruiterPortal />}
        {user.role === 'HiringManager' && <HiringManagerDashboard />}
        {user.role === 'Administrator' && <AdminPortal />}
      </main>

      {/* Mini Footer */}
      <footer style={{ borderTop: '1px solid var(--border-color)', padding: '1.5rem', textAlign: 'center', fontSize: '0.8rem', color: 'var(--text-muted)' }}>
        Talentio Recruitment & Talent Management Platform &copy; 2026. Built for SE205.3 Coursework.
      </footer>
    </div>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <MainAppContent />
    </AuthProvider>
  );
}
