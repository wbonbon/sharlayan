import './App.css';

import React, { FC } from 'react';

import { EventHost } from './EventHost';
import logo from './logo.svg';

const App: FC = () => {
  return (
    <div className="App">
      <header className="App-header">
        <img src={logo} className="App-logo" alt="logo" />
        <p>
          Edit <code>src/App.tsx</code> and save to reload.
        </p>
        <a className="App-link" href="https://reactjs.org" target="_blank" rel="noopener noreferrer">
          Learn React
        </a>
      </header>
      <EventHost />
    </div>
  );
};

export default App;
