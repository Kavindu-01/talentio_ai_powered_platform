import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';

export default function AdminPortal() {
  const { apiFetch } = useAuth();
  
  // States
  const [users, setUsers] = useState([]);
  const [logs, setLogs] = useState([]);
  const [organizations, setOrganizations] = useState([]);
  const [summary, setSummary] = useState(null);

  // Active sub-tab: 'users', 'organizations', 'analytics', 'monitoring', 'audit'
  const [activeSubTab, setActiveSubTab] = useState('users');

  // Edit user role form
  const [editingUserId, setEditingUserId] = useState(null);
  const [editRoleValue, setEditRoleValue] = useState(0); // Enum index

  // Org form state
  const [showOrgModal, setShowOrgModal] = useState(false);
  const [editingOrgId, setEditingOrgId] = useState(null);
  const [orgName, setOrgName] = useState('');
  const [orgAddress, setOrgAddress] = useState('');
  const [orgEmail, setOrgEmail] = useState('');
  const [orgPhone, setOrgPhone] = useState('');

  // Mock Monitoring metrics state that fluctuates
  const [cpuLoad, setCpuLoad] = useState(14);
  const [memoryUsage, setMemoryUsage] = useState(256);
  const [activeThreads, setActiveThreads] = useState(8);

  const roleNames = ['Candidate', 'Recruiter', 'HiringManager', 'Administrator'];

  useEffect(() => {
    fetchUsers();
    fetchAuditLogs();
    fetchAnalyticsSummary();
    fetchOrganizations();

    // Fluctuating system metrics simulator
    const interval = setInterval(() => {
      setCpuLoad(Math.floor(10 + Math.random() * 15)); // 10% - 25%
      setMemoryUsage(Math.floor(250 + Math.random() * 20)); // 250MB - 270MB
      setActiveThreads(Math.floor(5 + Math.random() * 6)); // 5 - 11 threads
    }, 3000);

    return () => clearInterval(interval);
  }, []);

  const fetchUsers = async () => {
    try {
      const res = await apiFetch('user');
      if (res.ok) {
        const data = await res.json();
        setUsers(data);
      }
    } catch (e) {
      console.error("Failed to fetch users", e);
    }
  };

  const fetchAuditLogs = async () => {
    try {
      const res = await apiFetch('user/audit-logs');
      if (res.ok) {
        const data = await res.json();
        setLogs(data);
      }
    } catch (e) {
      console.error("Failed to fetch audit logs", e);
    }
  };

  const fetchAnalyticsSummary = async () => {
    try {
      const res = await apiFetch('analytics/dashboard-summary');
      if (res.ok) {
        const data = await res.json();
        setSummary(data);
      }
    } catch (e) {
      console.error("Failed to fetch analytics summary", e);
    }
  };

  const fetchOrganizations = async () => {
    try {
      const res = await apiFetch('organization');
      if (res.ok) {
        const data = await res.json();
        setOrganizations(data);
      }
    } catch (e) {
      console.error("Failed to fetch organizations", e);
    }
  };

  const handleUpdateRole = async (userId) => {
    try {
      const res = await apiFetch(`user/${userId}/role`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ role: parseInt(editRoleValue) }),
      });

      if (res.ok) {
        fetchUsers();
        fetchAuditLogs();
        setEditingUserId(null);
        alert('User role updated successfully.');
      } else {
        const data = await res.json();
        alert(data.message || 'Failed to update user role');
      }
    } catch (e) {
      console.error(e);
      alert('Error updating role');
    }
  };

  const handleApproveUser = async (userId) => {
    try {
      const res = await apiFetch(`user/${userId}/approve`, {
        method: 'PUT',
      });

      if (res.ok) {
        fetchUsers();
        fetchAuditLogs();
        const data = await res.json();
        alert(data.message || 'User approved successfully.');
      } else {
        const data = await res.json();
        alert(data.message || 'Failed to approve user');
      }
    } catch (e) {
      console.error(e);
      alert('Error approving user');
    }
  };

  const handleDeleteUser = async (userId) => {
    if (!confirm('Are you sure you want to delete this user?')) return;

    try {
      const res = await apiFetch(`user/${userId}`, {
        method: 'DELETE',
      });

      if (res.ok) {
        fetchUsers();
        fetchAuditLogs();
        alert('User account deleted.');
      } else {
        const data = await res.json();
        alert(data.message || 'Failed to delete user');
      }
    } catch (e) {
      console.error(e);
    }
  };

  const handleSaveOrg = async (e) => {
    e.preventDefault();
    try {
      const isEdit = !!editingOrgId;
      const url = isEdit ? `organization/${editingOrgId}` : 'organization';
      const method = isEdit ? 'PUT' : 'POST';

      const res = await apiFetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: orgName,
          address: orgAddress,
          contactEmail: orgEmail,
          phone: orgPhone
        })
      });

      if (res.ok) {
        fetchOrganizations();
        fetchAuditLogs();
        setShowOrgModal(false);
        setOrgName('');
        setOrgAddress('');
        setOrgEmail('');
        setOrgPhone('');
        setEditingOrgId(null);
        alert(isEdit ? 'Organization updated.' : 'Organization created.');
      } else {
        alert('Failed to save organization');
      }
    } catch (e) {
      console.error(e);
    }
  };

  const openOrgEdit = (org) => {
    setEditingOrgId(org.Id);
    setOrgName(org.Name);
    setOrgAddress(org.Address);
    setOrgEmail(org.ContactEmail);
    setOrgPhone(org.Phone);
    setShowOrgModal(true);
  };

  const handleDeleteOrg = async (orgId) => {
    if (!confirm('Are you sure you want to delete this organization?')) return;
    try {
      const res = await apiFetch(`organization/${orgId}`, {
        method: 'DELETE'
      });
      if (res.ok) {
        fetchOrganizations();
        fetchAuditLogs();
        alert('Organization deleted.');
      }
    } catch (e) {
      console.error(e);
    }
  };

  return (
    <div>
      <div className="dashboard-header">
        <div className="dashboard-title">
          <h1>Administration Dashboard</h1>
          <p>Configure user roles, analyze recruitment metrics, manage organization settings, and inspect system health.</p>
        </div>
      </div>

      {/* Tabs */}
      <div style={{ display: 'flex', gap: '1rem', borderBottom: '1px solid var(--border-color)', marginBottom: '2rem', flexWrap: 'wrap' }}>
        <button
          className="btn"
          style={{
            borderBottom: activeSubTab === 'users' ? '2px solid var(--primary)' : 'none',
            color: activeSubTab === 'users' ? 'var(--text-primary)' : 'var(--text-secondary)',
            background: 'none',
            borderRadius: '0',
            fontWeight: '600'
          }}
          onClick={() => setActiveSubTab('users')}
        >
          User Management ({users.length})
        </button>
        <button
          className="btn"
          style={{
            borderBottom: activeSubTab === 'organizations' ? '2px solid var(--primary)' : 'none',
            color: activeSubTab === 'organizations' ? 'var(--text-primary)' : 'var(--text-secondary)',
            background: 'none',
            borderRadius: '0',
            fontWeight: '600'
          }}
          onClick={() => setActiveSubTab('organizations')}
        >
          Organizations & Departments ({organizations.length})
        </button>
        <button
          className="btn"
          style={{
            borderBottom: activeSubTab === 'analytics' ? '2px solid var(--primary)' : 'none',
            color: activeSubTab === 'analytics' ? 'var(--text-primary)' : 'var(--text-secondary)',
            background: 'none',
            borderRadius: '0',
            fontWeight: '600'
          }}
          onClick={() => { setActiveSubTab('analytics'); fetchAnalyticsSummary(); }}
        >
          System Analytics
        </button>
        <button
          className="btn"
          style={{
            borderBottom: activeSubTab === 'monitoring' ? '2px solid var(--primary)' : 'none',
            color: activeSubTab === 'monitoring' ? 'var(--text-primary)' : 'var(--text-secondary)',
            background: 'none',
            borderRadius: '0',
            fontWeight: '600'
          }}
          onClick={() => setActiveSubTab('monitoring')}
        >
          System Monitoring
        </button>
        <button
          className="btn"
          style={{
            borderBottom: activeSubTab === 'audit' ? '2px solid var(--primary)' : 'none',
            color: activeSubTab === 'audit' ? 'var(--text-primary)' : 'var(--text-secondary)',
            background: 'none',
            borderRadius: '0',
            fontWeight: '600'
          }}
          onClick={() => { setActiveSubTab('audit'); fetchAuditLogs(); }}
        >
          System Audit Logs ({logs.length})
        </button>
      </div>

      {/* Users Tab */}
      {activeSubTab === 'users' && (
        <div className="card" style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-secondary)' }}>
                <th style={{ padding: '1rem 0.5rem' }}>Name</th>
                <th style={{ padding: '1rem 0.5rem' }}>Email</th>
                <th style={{ padding: '1rem 0.5rem' }}>Current Role</th>
                <th style={{ padding: '1rem 0.5rem' }}>Approval Status</th>
                <th style={{ padding: '1rem 0.5rem' }}>Joined Date</th>
                <th style={{ padding: '1rem 0.5rem', textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => {
                const roleClass = `role-tag ${u.Role === 3 ? 'admin' : u.Role === 2 ? 'manager' : u.Role === 1 ? 'recruiter' : ''}`;
                
                return (
                  <tr key={u.Id} style={{ borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                    <td style={{ padding: '1rem 0.5rem', fontWeight: '500' }}>{u.Name}</td>
                    <td style={{ padding: '1rem 0.5rem', color: 'var(--text-secondary)' }}>{u.Email}</td>
                    <td style={{ padding: '1rem 0.5rem' }}>
                      {editingUserId === u.Id ? (
                        <div style={{ display: 'flex', gap: '0.5rem' }}>
                          <select
                            className="form-control"
                            style={{ padding: '0.2rem 0.5rem', width: '140px', fontSize: '0.8rem' }}
                            value={editRoleValue}
                            onChange={(e) => setEditRoleValue(e.target.value)}
                          >
                            <option value="0">Candidate</option>
                            <option value="1">Recruiter</option>
                            <option value="2">Hiring Manager</option>
                            <option value="3">Administrator</option>
                          </select>
                          <button className="btn btn-primary btn-sm" onClick={() => handleUpdateRole(u.Id)}>
                            Save
                          </button>
                          <button className="btn btn-secondary btn-sm" onClick={() => setEditingUserId(null)}>
                            Cancel
                          </button>
                        </div>
                      ) : (
                        <span className={roleClass}>{roleNames[u.Role]}</span>
                      )}
                    </td>
                    <td style={{ padding: '1rem 0.5rem' }}>
                      {u.IsApproved ? (
                        <span className="status-pill status-approved">Approved</span>
                      ) : (
                        <span className="status-pill status-rejected" style={{ background: 'rgba(239, 68, 68, 0.15)', color: '#F87171', border: '1px solid rgba(239, 68, 68, 0.3)' }}>
                          ⏳ Pending Approval
                        </span>
                      )}
                    </td>
                    <td style={{ padding: '1rem 0.5rem', color: 'var(--text-muted)' }}>
                      {new Date(u.CreatedAt).toLocaleDateString()}
                    </td>
                    <td style={{ padding: '1rem 0.5rem', textAlign: 'right' }}>
                      {editingUserId !== u.Id && (
                        <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                          {!u.IsApproved && (
                            <button
                              className="btn btn-primary btn-sm"
                              style={{ backgroundColor: 'var(--success)', borderColor: 'var(--success)' }}
                              onClick={() => handleApproveUser(u.Id)}
                            >
                              ✓ Approve Account
                            </button>
                          )}
                          <button
                            className="btn btn-secondary btn-sm"
                            onClick={() => { setEditingUserId(u.Id); setEditRoleValue(u.Role); }}
                          >
                            Edit Role
                          </button>
                          <button
                            className="btn btn-danger btn-sm"
                            onClick={() => handleDeleteUser(u.Id)}
                          >
                            Delete
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Organizations Tab */}
      {activeSubTab === 'organizations' && (
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
            <h3 style={{ fontSize: '1.1rem', color: 'var(--text-secondary)' }}>Organizations Directory</h3>
            <button className="btn btn-primary btn-sm" onClick={() => { setEditingOrgId(null); setOrgName(''); setOrgAddress(''); setOrgEmail(''); setOrgPhone(''); setShowOrgModal(true); }}>
              Add Organization
            </button>
          </div>

          <div className="dashboard-grid">
            {organizations.map((org) => (
              <div key={org.Id} className="card" style={{ display: 'flex', flexDirection: 'column', justifyContent: 'space-between' }}>
                <div>
                  <h3 style={{ fontSize: '1.2rem', marginBottom: '0.5rem' }}>{org.Name}</h3>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.4rem', fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
                    <span><strong>Address:</strong> {org.Address}</span>
                    <span><strong>Email:</strong> {org.ContactEmail}</span>
                    <span><strong>Phone:</strong> {org.Phone}</span>
                  </div>
                </div>
                <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1.5rem' }}>
                  <button className="btn btn-secondary btn-sm btn-block" onClick={() => openOrgEdit(org)}>
                    Edit Details
                  </button>
                  <button className="btn btn-danger btn-sm" onClick={() => handleDeleteOrg(org.Id)} style={{ padding: '0.4rem' }}>
                    Delete
                  </button>
                </div>
              </div>
            ))}
          </div>

          {/* Org Modal overlay */}
          {showOrgModal && (
            <div style={{
              position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
              background: 'rgba(0,0,0,0.7)', display: 'flex', justifyContent: 'center',
              alignItems: 'center', zIndex: 200, padding: '1rem'
            }}>
              <div className="card" style={{ width: '100%', maxWidth: '550px', background: 'var(--bg-secondary)', border: '1px solid var(--border-color)', position: 'relative' }}>
                <button
                  className="btn btn-secondary btn-sm"
                  style={{ position: 'absolute', top: '1rem', right: '1rem', padding: '0.2rem 0.5rem' }}
                  onClick={() => setShowOrgModal(false)}
                >
                  X
                </button>
                
                <h3 style={{ fontSize: '1.2rem', marginBottom: '1.5rem' }}>{editingOrgId ? 'Edit Organization' : 'Add New Organization'}</h3>

                <form onSubmit={handleSaveOrg}>
                  <div className="form-group">
                    <label className="form-label" htmlFor="org-name">Company Name</label>
                    <input
                      id="org-name"
                      type="text"
                      className="form-control"
                      value={orgName}
                      onChange={(e) => setOrgName(e.target.value)}
                      required
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label" htmlFor="org-email">Contact Email</label>
                    <input
                      id="org-email"
                      type="email"
                      className="form-control"
                      value={orgEmail}
                      onChange={(e) => setOrgEmail(e.target.value)}
                      required
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label" htmlFor="org-phone">Contact Phone</label>
                    <input
                      id="org-phone"
                      type="text"
                      className="form-control"
                      value={orgPhone}
                      onChange={(e) => setOrgPhone(e.target.value)}
                      required
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label" htmlFor="org-address">Street Address</label>
                    <input
                      id="org-address"
                      type="text"
                      className="form-control"
                      value={orgAddress}
                      onChange={(e) => setOrgAddress(e.target.value)}
                      required
                    />
                  </div>

                  <div style={{ display: 'flex', gap: '0.5rem', marginTop: '2rem' }}>
                    <button type="button" className="btn btn-secondary btn-block" onClick={() => setShowOrgModal(false)}>
                      Cancel
                    </button>
                    <button type="submit" className="btn btn-primary btn-block">
                      Save Record
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Analytics Tab */}
      {activeSubTab === 'analytics' && summary && (
        <div>
          {/* Widgets grid */}
          <div className="stats-container">
            <div className="stat-widget">
              <span className="stat-title">Open Jobs</span>
              <span className="stat-value">{summary.TotalJobs}</span>
              <span className="stat-desc">Active listings</span>
            </div>
            <div className="stat-widget">
              <span className="stat-title">Registered Candidates</span>
              <span className="stat-value">{summary.TotalCandidates}</span>
              <span className="stat-desc">Talent pipeline size</span>
            </div>
            <div className="stat-widget">
              <span className="stat-title">Submitted Applications</span>
              <span className="stat-value">{summary.TotalApplications}</span>
              <span className="stat-desc">Overall candidates applied</span>
            </div>
            <div className="stat-widget">
              <span className="stat-title">Interviews Scheduled</span>
              <span className="stat-value">{summary.TotalInterviews}</span>
              <span className="stat-desc">Meetings scheduled</span>
            </div>
            <div className="stat-widget" style={{ borderColor: 'rgba(99, 102, 241, 0.4)' }}>
              <span className="stat-title" style={{ color: 'var(--accent)' }}>Average AI Match</span>
              <span className="stat-value">{summary.AverageAIScore}%</span>
              <span className="stat-desc">Platform score suitability</span>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '2rem', marginTop: '2rem' }}>
            {/* Status Breakdown Panel */}
            <div className="card">
              <h3 style={{ fontSize: '1.1rem', marginBottom: '1.5rem', color: 'var(--text-secondary)' }}>
                Application Pipeline Breakdown
              </h3>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
                {summary.StatusBreakdown && summary.StatusBreakdown.map((sb, idx) => {
                  const percent = summary.TotalApplications > 0
                    ? Math.round((sb.Count / summary.TotalApplications) * 100)
                    : 0;
                  
                  const colors = {
                    'applied': 'var(--primary)',
                    'shortlisted': 'var(--secondary)',
                    'interviewing': 'var(--warning)',
                    'offered': 'var(--success)',
                    'rejected': 'var(--danger)'
                  };
                  const color = colors[sb.Status.toLowerCase()] || 'var(--text-muted)';

                  return (
                    <div key={idx}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.85rem', marginBottom: '0.4rem' }}>
                        <span style={{ fontWeight: '600' }}>{sb.Status}</span>
                        <span>{sb.Count} ({percent}%)</span>
                      </div>
                      <div style={{ background: 'rgba(255,255,255,0.05)', borderRadius: '9999px', height: '8px', overflow: 'hidden' }}>
                        <div style={{ width: `${percent}%`, backgroundColor: color, height: '100%' }}></div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>

            {/* Performance Over Time chart */}
            <div className="card" style={{ display: 'flex', flexDirection: 'column', justifyContent: 'space-between' }}>
              <div>
                <h3 style={{ fontSize: '1.1rem', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>
                  Recruitment Activities Timeline
                </h3>
                <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginBottom: '1.5rem' }}>
                  Daily application volume distribution
                </p>
              </div>

              {/* Simulated SVG Chart */}
              <div style={{ height: '150px', display: 'flex', alignItems: 'flex-end', gap: '1.5rem', padding: '0 1rem' }}>
                {summary.Trend && summary.Trend.length > 0 ? (
                  summary.Trend.map((t, idx) => {
                    const maxHeight = 120;
                    const maxVal = Math.max(...summary.Trend.map(x => x.Count)) || 1;
                    const height = (t.Count / maxVal) * maxHeight;

                    return (
                      <div key={idx} style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '0.5rem' }}>
                        <span style={{ fontSize: '0.75rem', fontWeight: '700', color: 'var(--accent)' }}>{t.Count}</span>
                        <div style={{
                          width: '100%',
                          height: `${height}px`,
                          background: 'linear-gradient(to top, var(--primary), var(--secondary))',
                          borderRadius: '4px 4px 0 0',
                          minHeight: '4px'
                        }}></div>
                        <span style={{ fontSize: '0.65rem', color: 'var(--text-muted)' }}>{t.Date.substring(5)}</span>
                      </div>
                    );
                  })
                ) : (
                  <p style={{ width: '100%', textAlign: 'center', color: 'var(--text-muted)', fontSize: '0.85rem' }}>
                    Seeded applications active. Submit more applications to view daily trend values.
                  </p>
                )}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* System Monitoring Tab */}
      {activeSubTab === 'monitoring' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '2rem' }}>
          
          {/* Live Performance Panel */}
          <div className="card">
            <h3 style={{ fontSize: '1.1rem', marginBottom: '1.5rem', color: 'var(--text-secondary)' }}>
              Live System Performance Metrics
            </h3>
            
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1.5rem' }}>
              
              {/* CPU Load */}
              <div style={{ textAlign: 'center' }}>
                <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>CPU Resource Load</span>
                <div style={{ fontSize: '2.2rem', fontWeight: '800', margin: '0.5rem 0', color: cpuLoad > 20 ? 'var(--warning)' : 'var(--primary)' }}>
                  {cpuLoad}%
                </div>
                <div style={{ background: 'rgba(255,255,255,0.05)', height: '6px', borderRadius: '3px', overflow: 'hidden' }}>
                  <div style={{ width: `${cpuLoad}%`, backgroundColor: cpuLoad > 20 ? 'var(--warning)' : 'var(--primary)', height: '100%', transition: 'width 0.5s ease' }}></div>
                </div>
              </div>

              {/* Memory Allocation */}
              <div style={{ textAlign: 'center' }}>
                <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Memory Allocation</span>
                <div style={{ fontSize: '2.2rem', fontWeight: '800', margin: '0.5rem 0', color: 'var(--secondary)' }}>
                  {memoryUsage} MB
                </div>
                <div style={{ background: 'rgba(255,255,255,0.05)', height: '6px', borderRadius: '3px', overflow: 'hidden' }}>
                  <div style={{ width: `${(memoryUsage/512)*100}%`, backgroundColor: 'var(--secondary)', height: '100%', transition: 'width 0.5s ease' }}></div>
                </div>
              </div>

              {/* Database State */}
              <div style={{ textAlign: 'center' }}>
                <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Database Connection Status</span>
                <div style={{ fontSize: '1.3rem', fontWeight: '700', margin: '1.1rem 0', color: 'var(--success)' }}>
                  SQLite Connected
                </div>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>recruitment.db database active</span>
              </div>

              {/* Network Threads */}
              <div style={{ textAlign: 'center' }}>
                <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Active HTTP Call Threads</span>
                <div style={{ fontSize: '2.2rem', fontWeight: '800', margin: '0.5rem 0', color: 'var(--accent)' }}>
                  {activeThreads}
                </div>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Active network processing logs</span>
              </div>

            </div>
          </div>

          {/* System Environment Config Panel */}
          <div className="card">
            <h3 style={{ fontSize: '1.1rem', marginBottom: '1rem', color: 'var(--text-secondary)' }}>
              Environment Details & Security Policies
            </h3>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1.5rem', fontSize: '0.85rem' }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                <span><strong>ASP.NET Core Server Version:</strong> .NET 8.0 SDK Runtime</span>
                <span><strong>API Port Configuration:</strong> http://localhost:5267</span>
                <span><strong>JWT Validation Expiry:</strong> 7 days (Symmetric HmacSha256)</span>
                <span><strong>Data Hashing Algorithm:</strong> BCrypt.Net Secure Hashing</span>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                <span><strong>Relational Database Engine:</strong> Microsoft Entity Framework SQLite Provider</span>
                <span><strong>AI Integration Model:</strong> Google Gemini 1.5 Flash API</span>
                <span><strong>Simulated Calendar Gateway:</strong> Local File logging directory</span>
                <span><strong>Simulated Notification Dispatcher:</strong> Simulated SMS/Email logs</span>
              </div>
            </div>
          </div>

        </div>
      )}

      {/* Audit Logs Tab */}
      {activeSubTab === 'audit' && (
        <div className="card" style={{ overflowX: 'auto', maxHeight: '70vh', overflowY: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.85rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-secondary)' }}>
                <th style={{ padding: '0.8rem 0.5rem', width: '180px' }}>Timestamp (UTC)</th>
                <th style={{ padding: '0.8rem 0.5rem', width: '160px' }}>Authorized Operator</th>
                <th style={{ padding: '0.8rem 0.5rem', width: '220px' }}>Action Category</th>
                <th style={{ padding: '0.8rem 0.5rem' }}>Audit Event Details</th>
              </tr>
            </thead>
            <tbody>
              {logs.map((l) => (
                <tr key={l.Id} style={{ borderBottom: '1px solid rgba(255,255,255,0.02)' }}>
                  <td style={{ padding: '0.8rem 0.5rem', color: 'var(--text-muted)' }}>
                    {new Date(l.Timestamp).toISOString().replace('T', ' ').substring(0, 19)}
                  </td>
                  <td style={{ padding: '0.8rem 0.5rem', fontWeight: '500' }}>{l.UserEmail}</td>
                  <td style={{ padding: '0.8rem 0.5rem' }}>
                    <span className="status-pill status-applied" style={{ fontSize: '0.65rem', padding: '0.15rem 0.4rem' }}>
                      {l.Action}
                    </span>
                  </td>
                  <td style={{ padding: '0.8rem 0.5rem', color: 'var(--text-secondary)' }}>{l.Details}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
