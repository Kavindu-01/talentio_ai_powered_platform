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
  const { user, logout, loading, apiFetch } = useAuth();
  const [showRegister, setShowRegister] = useState(false);
  const [showProfileModal, setShowProfileModal] = useState(false);

  // AI Chatbot Widget States
  const [chatOpen, setChatOpen] = useState(false);
  const [messages, setMessages] = useState([
    { sender: 'bot', text: 'Hello! I am your Talentio Talent Intelligence Assistant. How can I help you navigate the platform or manage recruitment tasks today?' }
  ]);
  const [inputText, setInputText] = useState('');
  const [sending, setSending] = useState(false);
  const messagesEndRef = React.useRef(null);

  const renderMessageText = (text) => {
    if (!text) return null;
    const lines = text.split('\n');
    let listItems = [];
    const elements = [];

    const flushList = (key) => {
      if (listItems.length > 0) {
        elements.push(<ul key={`list-${key}`} style={{ paddingLeft: '1.2rem', margin: '0.4rem 0', listStyleType: 'disc' }}>{listItems}</ul>);
        listItems = [];
      }
    };

    lines.forEach((line, idx) => {
      const parseBold = (str) => {
        const parts = str.split('**');
        return parts.map((part, i) => i % 2 === 1 ? <strong key={i} style={{ color: 'var(--primary)', fontWeight: 'bold' }}>{part}</strong> : part);
      };

      const listMatch = line.match(/^(\*|-|\d+\.)\s+(.*)$/);
      if (listMatch) {
        const itemContent = listMatch[2];
        listItems.push(
          <li key={`li-${idx}`} style={{ marginBottom: '0.3rem' }}>
            {parseBold(itemContent)}
          </li>
        );
      } else {
        flushList(idx);
        if (line.trim()) {
          elements.push(
            <p key={`p-${idx}`} style={{ margin: '0.4rem 0' }}>
              {parseBold(line)}
            </p>
          );
        }
      }
    });

    flushList('final');
    return elements;
  };

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    if (chatOpen) {
      scrollToBottom();
    }
  }, [messages, chatOpen]);

  const handleSendMessage = async (e) => {
    e.preventDefault();
    if (!inputText.trim()) return;

    const userMsg = { sender: 'user', text: inputText };
    setMessages(prev => [...prev, userMsg]);
    setInputText('');
    setSending(true);

    try {
      const historyPayload = messages.map(m => ({
        sender: m.sender === 'bot' ? 'bot' : 'user',
        text: m.text
      }));

      const res = await apiFetch('ai/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          message: userMsg.text,
          history: historyPayload
        })
      });

      if (res.ok) {
        const data = await res.json();
        setMessages(prev => [...prev, { sender: 'bot', text: data.response }]);
      } else {
        setMessages(prev => [...prev, { sender: 'bot', text: 'Sorry, I had trouble connecting. Please try again!' }]);
      }
    } catch (err) {
      console.error(err);
      setMessages(prev => [...prev, { sender: 'bot', text: 'An error occurred. Please try again!' }]);
    } finally {
      setSending(false);
    }
  };

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
        Talentio Recruitment & Talent Management Platform &copy; 2026.
      </footer>

      {/* Floating Chat Trigger Button */}
      <button
        className="chatbot-trigger"
        onClick={() => setChatOpen(!chatOpen)}
        title="Chat with Talentio AI Assistant"
        aria-label="Chat with Talentio AI Assistant"
      >
        💬
      </button>

      {/* Floating Chat Panel */}
      {chatOpen && (
        <div className="chatbot-panel" role="complementary" aria-label="Talentio Chat Assistant">
          <div className="chatbot-header">
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <span style={{ fontSize: '1.2rem' }}>✨</span>
              <strong style={{ fontSize: '0.9rem', color: 'var(--text-primary)' }}>Talentio AI Assistant</strong>
            </div>
            <button
              onClick={() => setChatOpen(false)}
              style={{ background: 'none', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer', fontSize: '1.1rem' }}
              title="Close chat panel"
              aria-label="Close chat panel"
            >
              &times;
            </button>
          </div>

          <div className="chatbot-messages">
            {messages.map((m, idx) => (
              <div
                key={idx}
                className={`chat-msg-bubble ${m.sender === 'user' ? 'chat-msg-user' : 'chat-msg-bot'}`}
              >
                {renderMessageText(m.text)}
              </div>
            ))}
            {sending && (
              <div className="chat-msg-bubble chat-msg-bot" style={{ opacity: 0.7 }}>
                Typing assistant response...
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>

          <form onSubmit={handleSendMessage} className="chatbot-input-area">
            <input
              type="text"
              placeholder="Ask me anything..."
              value={inputText}
              onChange={(e) => setInputText(e.target.value)}
              disabled={sending}
              aria-label="Ask assistant a question"
            />
            <button type="submit" className="chatbot-send-btn" disabled={sending} aria-label="Send Message">
              ➔
            </button>
          </form>
        </div>
      )}
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
