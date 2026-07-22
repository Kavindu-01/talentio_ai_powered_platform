import React, { createContext, useState, useEffect, useContext } from 'react';

const AuthContext = createContext(null);

export const API_URL = '/api';

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(localStorage.getItem('token') || null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (token) {
      // Decode JWT token basic details (just to get role and name)
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const isExpired = payload.exp * 1000 < Date.now();
        if (isExpired) {
          logout();
        } else {
          // Map standard C# claim keys to user fields
          const name = payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || payload.unique_name || "";
          const email = payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || payload.email || "";
          const role = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload.role || "";
          const id = payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || payload.nameid || "";

          setUser({ id, name, email, role });
        }
      } catch (e) {
        console.error("Failed to parse token", e);
        logout();
      }
    } else {
      setUser(null);
    }
    setLoading(false);
  }, [token]);

  const login = async (email, password) => {
    const res = await fetch(`${API_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });

    const data = await res.json();
    if (!res.ok) {
      throw new Error(data.message || 'Login failed');
    }

    localStorage.setItem('token', data.token);
    setToken(data.token);
    return data.user;
  };

  const register = async (name, email, password, role) => {
    // role is string like 'Candidate', 'Recruiter', 'HiringManager', 'Administrator'
    // C# expects enum int (Candidate=0, Recruiter=1, HiringManager=2, Administrator=3)
    const roleMapping = {
      'Candidate': 0,
      'Recruiter': 1,
      'HiringManager': 2,
      'Administrator': 3
    };

    const res = await fetch(`${API_URL}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, email, password, role: roleMapping[role] }),
    });

    const data = await res.json();
    if (!res.ok) {
      throw new Error(data.message || 'Registration failed');
    }
    return data;
  };

  const logout = () => {
    localStorage.removeItem('token');
    setToken(null);
    setUser(null);
  };

  const apiFetch = async (endpoint, options = {}) => {
    const headers = { ...options.headers };
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const res = await fetch(`${API_URL}/${endpoint}`, {
      ...options,
      headers,
    });

    if (res.status === 401) {
      logout();
      throw new Error('Session expired. Please log in again.');
    }

    return res;
  };

  return (
    <AuthContext.Provider value={{ user, token, loading, login, register, logout, apiFetch }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
