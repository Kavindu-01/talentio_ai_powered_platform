import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';

export default function ProfileModal({ isOpen, onClose }) {
  const { user, apiFetch } = useAuth();
  const [activeTab, setActiveTab] = useState('profile'); // 'profile' | 'resume'
  
  // Base User State
  const [name, setName] = useState(user?.name || '');
  const [avatarUrl, setAvatarUrl] = useState(user?.avatarUrl || '');
  
  // Candidate Resume State
  const [title, setTitle] = useState('Software Engineer');
  const [phone, setPhone] = useState('+1 (555) 019-2834');
  const [location, setLocation] = useState('Colombo, Sri Lanka');
  const [linkedIn, setLinkedIn] = useState('linkedin.com/in/profile');
  const [gitHub, setGitHub] = useState('github.com/profile');
  const [bio, setBio] = useState('');
  const [skills, setSkills] = useState('C#, .NET Core, React, SQL, JavaScript, HTML, CSS, Git, Web API, Docker');
  const [experience, setExperience] = useState('Senior Software Engineer - Tech Solutions (2022 - Present)\n• Designed and delivered high-performance RESTful Web APIs using C# and .NET Core.\n• Built responsive frontend interfaces using React and modern CSS styling.\n\nSoftware Developer - Innovations Ltd (2020 - 2022)\n• Developed database schemas and optimized SQL query performance.\n• Integrated third-party cloud services and automated CI/CD deployment pipelines.');
  
  // Multi-entry structured JSON states
  const [educationList, setEducationList] = useState([
    { school: 'University of Technology', degree: 'B.Sc. in Computer Science', year: '2016 - 2020', gpa: '3.8 GPA' }
  ]);
  const [projectList, setProjectList] = useState([
    { name: 'TalentAI Management Platform', tech: 'C#, React, SQL', desc: 'An AI-powered recruitment and candidate evaluation engine.' }
  ]);

  const [saving, setSaving] = useState(false);
  const [uploadingAvatar, setUploadingAvatar] = useState(false);
  const [success, setSuccess] = useState('');
  const [error, setError] = useState('');
  const [showPdfPreview, setShowPdfPreview] = useState(false);

  // Preset Avatar Avatars
  const presetAvatars = [
    'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80',
    'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&auto=format&fit=crop&q=80',
    'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150&auto=format&fit=crop&q=80',
    'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150&auto=format&fit=crop&q=80',
    'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=150&auto=format&fit=crop&q=80',
    'https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?w=150&auto=format&fit=crop&q=80'
  ];

  const handleFileUpload = async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    setUploadingAvatar(true);
    setError('');
    
    try {
      const formData = new FormData();
      formData.append('file', file);

      const res = await apiFetch('user/upload-avatar', {
        method: 'POST',
        body: formData
      });

      if (!res.ok) {
        const data = await res.json();
        throw new Error(data.message || 'Failed to upload profile picture');
      }

      const data = await res.json();
      setAvatarUrl(data.avatarUrl);
      setSuccess('Profile picture uploaded successfully!');
      setTimeout(() => setSuccess(''), 2000);
    } catch (err) {
      setError(err.message || 'Error uploading file');
    } finally {
      setUploadingAvatar(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      fetchCurrentProfile();
    }
  }, [isOpen]);

  const fetchCurrentProfile = async () => {
    try {
      if (user?.role === 'Candidate') {
        const res = await apiFetch('candidate/profile');
        if (res.ok) {
          const data = await res.json();
          setName(data.User?.Name || user.name);
          setAvatarUrl(data.User?.AvatarUrl || '');
          setTitle(data.Title || 'Software Engineer');
          setPhone(data.Phone || '');
          setLocation(data.Location || '');
          setLinkedIn(data.LinkedIn || '');
          setGitHub(data.GitHub || '');
          setBio(data.Bio || '');
          setSkills(data.Skills || '');
          setExperience(data.Experience || '');
          if (data.Education) {
            try { setEducationList(JSON.parse(data.Education)); } catch { }
          }
          if (data.Projects) {
            try { setProjectList(JSON.parse(data.Projects)); } catch { }
          }
        }
      } else {
        const res = await apiFetch('user/my-profile');
        if (res.ok) {
          const data = await res.json();
          setName(data.Name || user.name);
          setAvatarUrl(data.AvatarUrl || '');
        }
      }
    } catch (e) {
      console.error("Failed to load profile", e);
    }
  };

  const handleSave = async (e) => {
    e.preventDefault();
    setSaving(true);
    setError('');
    setSuccess('');

    try {
      if (user?.role === 'Candidate') {
        const payload = {
          name,
          avatarUrl,
          title,
          phone,
          location,
          linkedIn,
          gitHub,
          bio,
          skills,
          experience,
          education: JSON.stringify(educationList),
          projects: JSON.stringify(projectList)
        };

        const res = await apiFetch('candidate/profile', {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload)
        });

        if (!res.ok) {
          const data = await res.json();
          throw new Error(data.message || 'Failed to update candidate profile');
        }
      } else {
        const res = await apiFetch('user/my-profile', {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ name, avatarUrl })
        });

        if (!res.ok) {
          const data = await res.json();
          throw new Error(data.message || 'Failed to update profile');
        }
      }

      setSuccess('Profile customization saved successfully!');
      setTimeout(() => {
        setSuccess('');
        onClose();
        window.location.reload(); // Refresh to reflect name/avatar in navbar
      }, 1200);
    } catch (err) {
      setError(err.message || 'Error saving updates');
    } finally {
      setSaving(false);
    }
  };

  const handleAddEducation = () => {
    setEducationList([...educationList, { school: '', degree: '', year: '', gpa: '' }]);
  };

  const handleRemoveEducation = (index) => {
    setEducationList(educationList.filter((_, idx) => idx !== index));
  };

  const handleEducationChange = (index, field, value) => {
    const updated = [...educationList];
    updated[index][field] = value;
    setEducationList(updated);
  };

  const handleAddProject = () => {
    setProjectList([...projectList, { name: '', tech: '', desc: '' }]);
  };

  const handleRemoveProject = (index) => {
    setProjectList(projectList.filter((_, idx) => idx !== index));
  };

  const handleProjectChange = (index, field, value) => {
    const updated = [...projectList];
    updated[index][field] = value;
    setProjectList(updated);
  };

  const handlePrintPdf = () => {
    const printContent = document.getElementById('resume-print-sheet-content');
    if (!printContent) return;

    const printWindow = window.open('', '_blank', 'width=900,height=1000');
    if (!printWindow) {
      window.print();
      return;
    }

    printWindow.document.write(`
      <!DOCTYPE html>
      <html>
        <head>
          <title>${name || 'Candidate'} - Resume</title>
          <style>
            body { font-family: Arial, Helvetica, sans-serif; margin: 0; padding: 25px; color: #1E293B; background: #FFFFFF; }
            @page { margin: 1.2cm; size: A4; }
            h1 { font-size: 2.2rem; margin: 0; color: #0F172A; font-weight: 800; }
            h3 { font-size: 1rem; color: #0F172A; border-bottom: 1px solid #CBD5E1; padding-bottom: 4px; text-transform: uppercase; letter-spacing: 0.05em; margin-top: 1.2rem; margin-bottom: 0.6rem; }
            p { font-size: 0.9rem; line-height: 1.6; color: #334155; margin: 0; }
            .skill-tag { background: #EFF6FF; color: #1D4ED8; border: 1px solid #BFDBFE; padding: 4px 10px; border-radius: 4px; font-size: 12px; font-weight: 600; display: inline-block; margin: 2px; }
          </style>
        </head>
        <body>
          ${printContent.innerHTML}
        </body>
      </html>
    `);
    printWindow.document.close();
    printWindow.focus();
    setTimeout(() => {
      printWindow.print();
      printWindow.close();
    }, 300);
  };

  const getModalTitle = () => {
    switch (user?.role) {
      case 'Candidate':
        return 'Candidate Profile & Resume Builder';
      case 'Recruiter':
        return 'Recruiter Account Customization';
      case 'HiringManager':
        return 'Hiring Manager Profile Customization';
      case 'Administrator':
        return 'System Admin Profile Customization';
      default:
        return 'Account Profile Customization';
    }
  };

  const getModalSubtitle = () => {
    switch (user?.role) {
      case 'Candidate':
        return 'Customize your profile photo, personal info, work experience, and generate a professional PDF resume.';
      case 'Recruiter':
        return 'Customize your recruiter display name and account profile photo.';
      case 'HiringManager':
        return 'Customize your manager display name and account profile photo.';
      case 'Administrator':
        return 'Customize your administrator display name and account profile photo.';
      default:
        return 'Customize your display name and account profile photo.';
    }
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(0,0,0,0.75)', zIndex: 1000,
      display: 'flex', justifyContent: 'center', alignItems: 'center',
      padding: '1rem', backdropFilter: 'blur(4px)'
    }}>
      <div className="card" style={{
        width: '100%', maxWidth: '850px', maxHeight: '90vh',
        overflowY: 'auto', borderRadius: '12px', border: '1px solid var(--border-color)',
        boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.5)'
      }}>
        {/* Header Bar */}
        <div style={{
          display: 'flex', justifyContent: 'space-between', alignItems: 'center',
          borderBottom: '1px solid var(--border-color)', paddingBottom: '1rem', marginBottom: '1.25rem'
        }}>
          <div>
            <h2 style={{ fontSize: '1.4rem', margin: 0, color: 'var(--text-primary)' }}>
              {getModalTitle()}
            </h2>
            <p style={{ margin: '0.2rem 0 0 0', fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
              {getModalSubtitle()}
            </p>
          </div>
          <button type="button" className="btn btn-secondary btn-sm" onClick={onClose} style={{ padding: '0.3rem 0.6rem' }}>
            &times; Close
          </button>
        </div>

        {/* Navigation Tabs for Candidate */}
        {user?.role === 'Candidate' && (
          <div style={{ display: 'flex', gap: '1rem', borderBottom: '1px solid var(--border-color)', marginBottom: '1.5rem' }}>
            <button
              type="button"
              className="btn"
              style={{
                borderBottom: activeTab === 'profile' ? '2px solid var(--primary)' : 'none',
                color: activeTab === 'profile' ? 'var(--text-primary)' : 'var(--text-secondary)',
                background: 'none', borderRadius: 0, fontWeight: '600'
              }}
              onClick={() => setActiveTab('profile')}
            >
              Account & Bio
            </button>
            <button
              type="button"
              className="btn"
              style={{
                borderBottom: activeTab === 'resume' ? '2px solid var(--primary)' : 'none',
                color: activeTab === 'resume' ? 'var(--text-primary)' : 'var(--text-secondary)',
                background: 'none', borderRadius: 0, fontWeight: '600'
              }}
              onClick={() => setActiveTab('resume')}
            >
              Interactive Resume Builder & PDF Exporter
            </button>
          </div>
        )}

        {error && <div className="alert alert-danger" style={{ marginBottom: '1rem' }}>{error}</div>}
        {success && <div className="alert alert-success" style={{ marginBottom: '1rem' }}>{success}</div>}

        <form onSubmit={handleSave}>
          {/* Tab 1: Account Profile & Avatars */}
          {(activeTab === 'profile' || user?.role !== 'Candidate') && (
            <div>
              {/* Profile Photo / Avatar Picker */}
              <div className="form-group" style={{ marginBottom: '1.5rem' }}>
                <label className="form-label">Profile Avatar / Photo</label>
                <div style={{ display: 'flex', alignItems: 'center', gap: '1.25rem', marginTop: '0.5rem' }}>
                  <img
                    src={avatarUrl || 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80'}
                    alt="Avatar"
                    style={{ width: '75px', height: '75px', borderRadius: '50%', objectFit: 'cover', border: '2px solid var(--primary)' }}
                  />
                  <div style={{ flex: 1 }}>
                    <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '0.6rem' }}>
                      Upload a photo file from your computer or select a preset:
                    </div>
                    
                    <input
                      type="file"
                      id="avatar-upload-input"
                      accept="image/png, image/jpeg, image/jpg, image/webp"
                      style={{ display: 'none' }}
                      onChange={handleFileUpload}
                    />
                    
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.75rem' }}>
                      <button
                        type="button"
                        className="btn btn-secondary btn-sm"
                        onClick={() => document.getElementById('avatar-upload-input').click()}
                        disabled={uploadingAvatar}
                        style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}
                      >
                        📷 {uploadingAvatar ? 'Uploading Image...' : 'Choose / Upload Profile Photo'}
                      </button>
                      <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>JPG, PNG, WEBP max 5MB</span>
                    </div>

                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: '600' }}>Presets:</span>
                      {presetAvatars.map((url, idx) => (
                        <img
                          key={idx}
                          src={url}
                          alt={`Preset ${idx}`}
                          style={{
                            width: '32px', height: '32px', borderRadius: '50%', cursor: 'pointer',
                            border: avatarUrl === url ? '2px solid var(--primary)' : '1px solid var(--border-color)',
                            opacity: avatarUrl === url ? 1 : 0.6
                          }}
                          onClick={() => setAvatarUrl(url)}
                        />
                      ))}
                    </div>
                  </div>
                </div>
              </div>

              <div className="form-group">
                <label className="form-label" htmlFor="user-name">Full Display Name</label>
                <input
                  id="user-name"
                  type="text"
                  className="form-control"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  required
                />
              </div>

              {user?.role === 'Candidate' && (
                <>
                  <div className="form-group">
                    <label className="form-label" htmlFor="user-title">Professional Title</label>
                    <input
                      id="user-title"
                      type="text"
                      className="form-control"
                      placeholder="e.g. Senior Software Engineer / Full Stack Developer"
                      value={title}
                      onChange={(e) => setTitle(e.target.value)}
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label" htmlFor="user-bio">Professional Biography Summary</label>
                    <textarea
                      id="user-bio"
                      className="form-control"
                      rows="3"
                      placeholder="Discuss your background, main focus areas, and primary career goals..."
                      value={bio}
                      onChange={(e) => setBio(e.target.value)}
                    />
                  </div>
                </>
              )}
            </div>
          )}

          {/* Tab 2: Interactive Resume Builder (Candidates) */}
          {activeTab === 'resume' && user?.role === 'Candidate' && (
            <div>
              {/* Contact Details */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
                <div className="form-group">
                  <label className="form-label">Phone Number</label>
                  <input
                    type="text"
                    className="form-control"
                    placeholder="+1 (555) 000-0000"
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">Location / Address</label>
                  <input
                    type="text"
                    className="form-control"
                    placeholder="City, Country"
                    value={location}
                    onChange={(e) => setLocation(e.target.value)}
                  />
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1.5rem' }}>
                <div className="form-group">
                  <label className="form-label">LinkedIn Profile URL</label>
                  <input
                    type="text"
                    className="form-control"
                    placeholder="linkedin.com/in/yourname"
                    value={linkedIn}
                    onChange={(e) => setLinkedIn(e.target.value)}
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">GitHub / Portfolio URL</label>
                  <input
                    type="text"
                    className="form-control"
                    placeholder="github.com/yourname"
                    value={gitHub}
                    onChange={(e) => setGitHub(e.target.value)}
                  />
                </div>
              </div>

              {/* Technical Skills */}
              <div className="form-group" style={{ marginBottom: '1.5rem' }}>
                <label className="form-label">Key Technical Skills (Comma Separated)</label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="C#, React, SQL, Python, AWS, Docker, Git"
                  value={skills}
                  onChange={(e) => setSkills(e.target.value)}
                />
              </div>

              {/* Work Experience */}
              <div className="form-group" style={{ marginBottom: '1.5rem' }}>
                <label className="form-label">Work Experience Summary & Bullet Points</label>
                <textarea
                  className="form-control"
                  rows="4"
                  placeholder="List your job history, company names, titles, and key accomplishments..."
                  value={experience}
                  onChange={(e) => setExperience(e.target.value)}
                />
              </div>

              {/* Dynamic Education Section */}
              <div style={{ borderTop: '1px solid var(--border-color)', paddingTop: '1rem', marginBottom: '1.5rem' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.75rem' }}>
                  <h4 style={{ fontSize: '1rem', margin: 0, color: 'var(--text-primary)' }}>Education & Qualifications</h4>
                  <button type="button" className="btn btn-secondary btn-sm" onClick={handleAddEducation}>
                    + Add School / Degree
                  </button>
                </div>

                {educationList.map((edu, idx) => (
                  <div key={idx} style={{
                    padding: '0.8rem', background: 'rgba(255,255,255,0.02)',
                    border: '1px solid var(--border-color)', borderRadius: '8px', marginBottom: '0.75rem'
                  }}>
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem', marginBottom: '0.5rem' }}>
                      <input
                        type="text" className="form-control" placeholder="Institution / University"
                        value={edu.school} onChange={(e) => handleEducationChange(idx, 'school', e.target.value)}
                      />
                      <input
                        type="text" className="form-control" placeholder="Degree / Field of Study"
                        value={edu.degree} onChange={(e) => handleEducationChange(idx, 'degree', e.target.value)}
                      />
                    </div>
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr auto', gap: '0.5rem' }}>
                      <input
                        type="text" className="form-control" placeholder="Year Range (e.g. 2018 - 2022)"
                        value={edu.year} onChange={(e) => handleEducationChange(idx, 'year', e.target.value)}
                      />
                      <input
                        type="text" className="form-control" placeholder="GPA / Grade (e.g. 3.8 GPA)"
                        value={edu.gpa} onChange={(e) => handleEducationChange(idx, 'gpa', e.target.value)}
                      />
                      <button type="button" className="btn btn-danger btn-sm" onClick={() => handleRemoveEducation(idx)}>
                        Remove
                      </button>
                    </div>
                  </div>
                ))}
              </div>

              {/* Dynamic Key Projects Section */}
              <div style={{ borderTop: '1px solid var(--border-color)', paddingTop: '1rem', marginBottom: '1.5rem' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.75rem' }}>
                  <h4 style={{ fontSize: '1rem', margin: 0, color: 'var(--text-primary)' }}>Key Projects & Certifications</h4>
                  <button type="button" className="btn btn-secondary btn-sm" onClick={handleAddProject}>
                    + Add Project
                  </button>
                </div>

                {projectList.map((proj, idx) => (
                  <div key={idx} style={{
                    padding: '0.8rem', background: 'rgba(255,255,255,0.02)',
                    border: '1px solid var(--border-color)', borderRadius: '8px', marginBottom: '0.75rem'
                  }}>
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem', marginBottom: '0.5rem' }}>
                      <input
                        type="text" className="form-control" placeholder="Project Name"
                        value={proj.name} onChange={(e) => handleProjectChange(idx, 'name', e.target.value)}
                      />
                      <input
                        type="text" className="form-control" placeholder="Technologies Used"
                        value={proj.tech} onChange={(e) => handleProjectChange(idx, 'tech', e.target.value)}
                      />
                    </div>
                    <div style={{ display: 'flex', gap: '0.5rem' }}>
                      <input
                        type="text" className="form-control" placeholder="Brief project description & impact"
                        value={proj.desc} onChange={(e) => handleProjectChange(idx, 'desc', e.target.value)}
                      />
                      <button type="button" className="btn btn-danger btn-sm" onClick={() => handleRemoveProject(idx)}>
                        Remove
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Action Buttons Footer */}
          <div style={{
            borderTop: '1px solid var(--border-color)', paddingTop: '1.25rem', marginTop: '1.5rem',
            display: 'flex', justifyContent: 'space-between', alignItems: 'center'
          }}>
            <div>
              {user?.role === 'Candidate' && (
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowPdfPreview(true)}
                  style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}
                >
                  📄 Preview & Export PDF Resume
                </button>
              )}
            </div>

            <div style={{ display: 'flex', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={onClose}>
                Cancel
              </button>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Saving...' : 'Save Customization'}
              </button>
            </div>
          </div>
        </form>
      </div>

      {/* PDF Resume Print Preview Modal Overlay */}
      {showPdfPreview && (
        <div style={{
          position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
          backgroundColor: '#0F172A', zIndex: 2000, overflowY: 'auto', padding: '2rem 1rem'
        }}>
          <div style={{ maxWidth: '850px', margin: '0 auto', marginBottom: '1.5rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }} className="no-print">
            <h3 style={{ color: 'white', margin: 0 }}>Generated Professional Resume PDF</h3>
            <div style={{ display: 'flex', gap: '0.75rem' }}>
              <button className="btn btn-secondary" onClick={() => setShowPdfPreview(false)}>
                Back to Editing
              </button>
              <button className="btn btn-primary" onClick={handlePrintPdf}>
                🖨️ Save / Print as PDF
              </button>
            </div>
          </div>

          {/* Clean Printable Resume Sheet */}
          <div id="resume-print-sheet-content" className="resume-print-sheet" style={{
            maxWidth: '800px', margin: '0 auto', background: '#FFFFFF', color: '#1E293B',
            padding: '2.5rem', borderRadius: '8px', boxShadow: '0 10px 25px rgba(0,0,0,0.3)',
            fontFamily: 'Arial, Helvetica, sans-serif'
          }}>
            {/* Resume Header */}
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '3px solid #3B82F6', paddingBottom: '1.5rem', marginBottom: '1.5rem' }}>
              <div>
                <h1 style={{ fontSize: '2.2rem', margin: 0, color: '#0F172A', fontWeight: '800' }}>{name}</h1>
                <p style={{ fontSize: '1.1rem', color: '#2563EB', margin: '0.2rem 0 0 0', fontWeight: '600' }}>{title}</p>
              </div>
              {avatarUrl && (
                <img src={avatarUrl} alt="Avatar" style={{ width: '85px', height: '85px', borderRadius: '50%', objectFit: 'cover', border: '3px solid #3B82F6' }} />
              )}
            </div>

            {/* Resume Contact Strip */}
            <div style={{
              display: 'flex', flexWrap: 'wrap', gap: '1.25rem', fontSize: '0.85rem', color: '#475569',
              background: '#F1F5F9', padding: '0.75rem 1rem', borderRadius: '6px', marginBottom: '1.5rem'
            }}>
              <span><strong>Email:</strong> {user?.email}</span>
              {phone && <span><strong>Phone:</strong> {phone}</span>}
              {location && <span><strong>Location:</strong> {location}</span>}
              {linkedIn && <span><strong>LinkedIn:</strong> {linkedIn}</span>}
              {gitHub && <span><strong>GitHub:</strong> {gitHub}</span>}
            </div>

            {/* Resume Summary */}
            {bio && (
              <div style={{ marginBottom: '1.5rem' }}>
                <h3 style={{ fontSize: '1rem', color: '#0F172A', borderBottom: '1px solid #CBD5E1', paddingBottom: '0.3rem', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                  Professional Profile
                </h3>
                <p style={{ fontSize: '0.9rem', lineHeight: '1.6', color: '#334155' }}>{bio}</p>
              </div>
            )}

            {/* Resume Technical Skills */}
            {skills && (
              <div style={{ marginBottom: '1.5rem' }}>
                <h3 style={{ fontSize: '1rem', color: '#0F172A', borderBottom: '1px solid #CBD5E1', paddingBottom: '0.3rem', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                  Technical Expertise
                </h3>
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem', marginTop: '0.5rem' }}>
                  {skills.split(',').map((s, idx) => (
                    <span key={idx} style={{
                      background: '#EFF6FF', color: '#1D4ED8', border: '1px solid #BFDBFE',
                      padding: '0.25rem 0.6rem', borderRadius: '4px', fontSize: '0.8rem', fontWeight: '600'
                    }}>
                      {s.trim()}
                    </span>
                  ))}
                </div>
              </div>
            )}

            {/* Resume Work History */}
            {experience && (
              <div style={{ marginBottom: '1.5rem' }}>
                <h3 style={{ fontSize: '1rem', color: '#0F172A', borderBottom: '1px solid #CBD5E1', paddingBottom: '0.3rem', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                  Professional Work History
                </h3>
                <p style={{ fontSize: '0.9rem', lineHeight: '1.6', color: '#334155', whiteSpace: 'pre-line' }}>{experience}</p>
              </div>
            )}

            {/* Resume Education */}
            {educationList.length > 0 && (
              <div style={{ marginBottom: '1.5rem' }}>
                <h3 style={{ fontSize: '1rem', color: '#0F172A', borderBottom: '1px solid #CBD5E1', paddingBottom: '0.3rem', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                  Education & Qualifications
                </h3>
                {educationList.map((edu, idx) => (
                  <div key={idx} style={{ marginBottom: '0.75rem', display: 'flex', justifyContent: 'space-between' }}>
                    <div>
                      <strong style={{ fontSize: '0.95rem', color: '#0F172A' }}>{edu.degree}</strong>
                      <div style={{ fontSize: '0.85rem', color: '#475569' }}>{edu.school} {edu.gpa ? `| ${edu.gpa}` : ''}</div>
                    </div>
                    <span style={{ fontSize: '0.85rem', color: '#64748B', fontWeight: '600' }}>{edu.year}</span>
                  </div>
                ))}
              </div>
            )}

            {/* Resume Projects & Certifications */}
            {projectList.length > 0 && (
              <div>
                <h3 style={{ fontSize: '1rem', color: '#0F172A', borderBottom: '1px solid #CBD5E1', paddingBottom: '0.3rem', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                  Key Projects & Accomplishments
                </h3>
                {projectList.map((proj, idx) => (
                  <div key={idx} style={{ marginBottom: '0.75rem' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <strong style={{ fontSize: '0.95rem', color: '#0F172A' }}>{proj.name}</strong>
                      <span style={{ fontSize: '0.8rem', color: '#2563EB', fontWeight: '600' }}>{proj.tech}</span>
                    </div>
                    <p style={{ fontSize: '0.85rem', color: '#475569', margin: '0.2rem 0 0 0' }}>{proj.desc}</p>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
